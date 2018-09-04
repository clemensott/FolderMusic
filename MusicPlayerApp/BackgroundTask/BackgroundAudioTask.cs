using LibraryLib;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;

namespace BackgroundTask
{
    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        private const long smtcLastPressedMinDeltaTicks = TimeSpan.TicksPerMillisecond * 300;

        private static BackgroundAudioTask instance;
        private static long smtcLastPressedTicks = 0;

        private BackgroundTaskDeferral deferral;
        private SystemMediaTransportControls smtc;

        private bool autoPlay = false, pauseAllowed = true, playNext = true;
        private Song openSong;

        public static BackgroundAudioTask Current { get { return instance; } }

        public bool IsPlaying
        {
            get
            {
                return pauseAllowed ? BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing : true;
            }
        }

        private double CurrentSongPositionTotalMilliseconds
        {
            get
            {
                return CurrentPlaylist.SongPositionPercent == 0 ? 1 :
                    CurrentPlaylist.SongPositionPercent * CurrentSong.NaturalDurationMilliseconds;
            }
        }

        private Song CurrentSong { get { return Library.Current.CurrentPlaylist.CurrentSong; } }

        private Playlist CurrentPlaylist { get { return Library.Current.CurrentPlaylist; } }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            FolderMusicDebug.SaveTextClass.Id = taskInstance.InstanceId.ToString();
            FolderMusicDebug.SaveTextClass.Current.SaveText("Run");

            instance = this;
            smtcLastPressedTicks = DateTime.Now.Ticks;

            ForegroundCommunicator.SetReceivedEvent();

            smtc = SystemMediaTransportControls.GetForCurrentView();
            deferral = taskInstance.GetDeferral();

            ActivateSystemMediaTransportControl();
            SetEvents(taskInstance);
            Ringer.Create();

            LoadCurrentSongAndLibrary();
        }

        private void SetEvents(IBackgroundTaskInstance taskInstance)
        {
            smtc.ButtonPressed += MediaTransportControlButtonPressed;

            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded += BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened += BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed += BackgroundMediaPlayer_MediaFailed;

            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += TaskCompleted;
        }

        private void LoadCurrentSongAndLibrary()
        {/*
            if (lastPressedButton == SystemMediaTransportControlsButton.Play)
            {
                await LibraryLib.CurrentSong.Current.Load();
                Play();
                await LoadLibraryData();
                return;
            }               //      */

            LoadLibraryData();
            SetCurrentSong();
            /*
            if (lastPressedButton == defaultPressedButton) lastPressedButton = 
            SystemMediaTransportControlsButton.Pause;
            else if (lastPressedButton != SystemMediaTransportControlsButton.Pause)
            {
                MediaTransportControlButtonPressed(lastPressedButton);
            }
            else Play();         //              */

            FolderMusicDebug.SaveTextClass.Current.AllowSaving();
        }

        private void LoadLibraryData()
        {
            Library.Current.Load();
            FolderMusicDebug.SaveTextClass.Current.SaveText("AfterLoad");

            SetLoopToBackgroundPlayer();

            ForegroundCommunicator.SendXmlText();
        }

        public void ActivateSystemMediaTransportControl()
        {
            smtc.IsEnabled = smtc.IsPauseEnabled = smtc.IsPlayEnabled =
                //smtc.IsRewindEnabled = smtc.IsFastForwardEnabled = 
                smtc.IsPreviousEnabled = smtc.IsNextEnabled = true;
        }

        public void SetLoopToBackgroundPlayer()
        {
            BackgroundMediaPlayer.Current.IsLoopingEnabled = CurrentPlaylist.Loop == LoopKind.Current;
        }

        public void Play()
        {
            autoPlay = true;

            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Closed ||
                BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped) SetCurrentSong(true);
            else if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing)
            {
                BackgroundMediaPlayer.Current.Volume = 0;
                BackgroundMediaPlayer.Current.Play();

                Volume0To1();
            }

            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;

            Ringer.Current.SetTimesIfIsDisposed();
        }

        private async void Volume0To1()
        {
            double step = 0.1;

            for (double i = step; i < 1; i += step)
            {
                BackgroundMediaPlayer.Current.Volume = Math.Sqrt(i);
                await Task.Delay(10);
            }
        }

        public void Pause()
        {
            if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Paused) Volume1To0AndPause();

            autoPlay = false;
            smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

            ForegroundCommunicator.SendPause();

            LibraryLib.CurrentSong.Current.SaveAsync();
        }

        private async void Volume1To0AndPause()
        {
            double step = 0.1;

            for (double i = 1; i > 0; i -= step)
            {
                BackgroundMediaPlayer.Current.Volume = Math.Sqrt(i);
                await Task.Delay(1);
            }

            BackgroundMediaPlayer.Current.Pause();
        }

        public void Previous()
        {
            playNext = false;
            CurrentPlaylist.SetPreviousSong();

            if (!CurrentSong.Failed) SetCurrentSong(IsPlaying);
        }

        public void Next(bool autoPlay)
        {
            Next(autoPlay, false);
        }

        public void Next(bool autoPlay, bool fromEnded)
        {
            playNext = true;
            bool stop = CurrentPlaylist.SetNextSong();
            this.autoPlay = fromEnded ? autoPlay && !stop : autoPlay;

            if (!CurrentSong.Failed) SetCurrentSong();
        }

        public void SetCurrentSong()
        {
            SetCurrentSong(autoPlay);
        }

        public void SetCurrentSong(bool autoPlay)
        {
            if (Ringer.Current.IsRinging) return;
            if (CurrentSong.IsEmptyOrLoading || CurrentSong == openSong) return;

            StorageFile file;

            this.autoPlay = autoPlay;
            pauseAllowed = !autoPlay;
            BackgroundMediaPlayer.Current.AutoPlay = false;

            try
            {
                file = CurrentSong.GetStorageFile();
                BackgroundMediaPlayer.Current.SetFileSource(file);
                FolderMusicDebug.SaveTextClass.Current.SaveText("Set", CurrentSong);
            }
            catch
            {
                FolderMusicDebug.SaveTextClass.Current.SaveText("Catch", CurrentSong);
                SkipSong(CurrentSong);
                Task.Delay(100).Wait();

                BackgroundMediaPlayer.Current.SetUriSource(null);

                if (playNext) Next(true);
                else Previous();
            }
        }

        private void SkipSong(Song song)
        {
            SkipSongs.AddSkipSongAndSave(song);

            ForegroundCommunicator.SendSkip();
        }

        private void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            FolderMusicDebug.SaveTextClass.Current.SaveText("Open", CurrentSong);

            if (Ringer.Current.IsRinging)
            {
                openSong = null;
                return;
            }

            pauseAllowed = true;
            openSong = CurrentSong;
            sender.Position = TimeSpan.FromMilliseconds(CurrentSongPositionTotalMilliseconds);

            if (autoPlay)
            {
                if (sender.Position.TotalMilliseconds == 1)
                {
                    sender.Volume = 1;
                    sender.Play();
                }
                else Play();
            }
            else smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

            playNext = true;

            UpdateSystemMediaTransportControl();

            if (!Library.IsLoaded) LoadLibraryData();

            ForegroundCommunicator.SendSongsIndexAndShuffleListIfIsShuffleComplete();

            LibraryLib.CurrentSong.Current.SaveAsync();
        }

        private void UpdateSystemMediaTransportControl()
        {
            if (smtc.DisplayUpdater.Type != MediaPlaybackType.Music)
            {
                smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
            }

            if (smtc.DisplayUpdater.MusicProperties.Title != CurrentSong.Title ||
                smtc.DisplayUpdater.MusicProperties.Artist != CurrentSong.Artist)
            {
                smtc.DisplayUpdater.MusicProperties.Title = CurrentSong.Title;
                smtc.DisplayUpdater.MusicProperties.Artist = CurrentSong.Artist;
                smtc.DisplayUpdater.Update();
            }
        }

        private void BackgroundMediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            if (Ringer.Current.IsRinging) return;

            FolderMusicDebug.SaveTextClass.Current.SaveText("Fail", CurrentSong);
            Task.Delay(100).Wait();

            if (args.Error == MediaPlayerError.Unknown)
            {
                SetCurrentSong();
                return;
            }

            CurrentSong.SetFailed();
            SkipSong(CurrentSong);

            if (playNext) Next(true);
            else Previous();
        }

        private void BackgroundMediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;

            if (Ringer.Current.IsRinging) return;
            FolderMusicDebug.SaveTextClass.Current.SaveText("EndedMedia", "SMTC-State: " + smtc.PlaybackStatus, CurrentSong);
            Next(true, true);
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("CurrentStateChanged");
            double curMillis = sender.Position.TotalMilliseconds;
            double natMillis = sender.NaturalDuration.TotalMilliseconds;

            if (curMillis >= natMillis) pauseAllowed = false;

            FolderMusicDebug.SaveTextClass.Current.SaveText("StateChanged", "Playerstate: " + sender.CurrentState,
                "SMTC-State: " + smtc.PlaybackStatus, "PlayerPosition [s]: " + sender.Position.TotalMilliseconds,
                "PlayerDuration [s]: " + sender.NaturalDuration.TotalMilliseconds, "PauseAllowed: " + pauseAllowed, CurrentSong);

            if (sender.CurrentState == MediaPlayerState.Playing) Play();
            else if (sender.CurrentState == MediaPlayerState.Paused && pauseAllowed) Pause();
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            long curTicks = DateTime.Now.Ticks;
            bool timePassed = smtcLastPressedTicks - curTicks < smtcLastPressedMinDeltaTicks;

            FolderMusicDebug.SaveTextClass.Current.SaveText("MTCPressed", "SmtcLastPressedTicks: " + smtcLastPressedTicks,
                "CurrentButton: " + args.Button, "LastPressedDeltaTicks: " + (curTicks - smtcLastPressedTicks),
                "TimePassed" + timePassed, CurrentSong);

            if (timePassed && smtcLastPressedTicks != 0)
            {
                if (!Library.IsLoaded)
                {
                    LibraryLib.CurrentSong.Current.Load();
                    FolderMusicDebug.SaveTextClass.Current.SaveText("SmtcLoad", CurrentSong);
                }

                MediaTransportControlButtonPressed(args.Button);
            }

            smtcLastPressedTicks = DateTime.Now.Ticks;
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControlsButton button)
        {
            switch (button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Play();
                    return;

                case SystemMediaTransportControlsButton.Pause:
                    Pause();
                    return;

                case SystemMediaTransportControlsButton.Previous:
                    Previous();
                    return;

                case SystemMediaTransportControlsButton.Next:
                    Next(IsPlaying);
                    return;
            }
        }

        private void TaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Ringer.Current.Dispose();
            LibraryLib.CurrentSong.Current.Save();
            //  SaveCurrentTicks("TaskCompleted.txt").Wait();

            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            Ringer.Current.Dispose();
            LibraryLib.CurrentSong.Current.Save();
            //  SaveCurrentTicks("OnCanceled.txt").Wait();

            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();
        }

        private async Task SaveCurrentTicks(string filename)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.
                CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(file, DateTime.Now.Ticks.ToString());
        }
    }
}
