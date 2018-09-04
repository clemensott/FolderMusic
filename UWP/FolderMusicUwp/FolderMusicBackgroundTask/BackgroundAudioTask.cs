using LibraryLib;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;

namespace FolderMusicBackgroundTask
{
    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        //private const SystemMediaTransportControlsButton defaultPressedButton = SystemMediaTransportControlsButton.ChannelDown;
        //private static SystemMediaTransportControlsButton lastPressedButton = defaultPressedButton;

        private static BackgroundAudioTask task;
       
        private BackgroundTaskDeferral deferral;
        private SystemMediaTransportControls systemMediaTransportControl;

        private bool autoPlay = false, pauseAllowed = true, playNext = true, gotReplayFromForeground;
        private int deactivateSMTC = 0;
        private long lastTicks;
        private Song openSong;

        public static BackgroundAudioTask Current { get { return task; } }

        public bool IsPlaying
        {
            get { return pauseAllowed ? BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing : true; }
        }

        private double CurrentSongPositionTotalMilliseconds
        {
            get
            {
                return Library.Current.CurrentPlaylist.SongPositionMilliseconds != 0 ? 
                    Library.Current.CurrentPlaylist.SongPositionMilliseconds : 1;
            }
        }

        private Song CurrentSong { get { return Library.Current.CurrentPlaylist.CurrentSong; } }

        private Playlist CurrentPlaylist { get { return Library.Current.CurrentPlaylist; } }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            FolderMusicUwpLib.SaveTextClass.SetId(taskInstance.InstanceId.ToString());
            FolderMusicUwpLib.SaveTextClass.SaveText("Run");

            task = this;

            lastTicks = DateTime.Now.Ticks;

            ForegroundCommunicator.SetReceivedEvent();

            systemMediaTransportControl = SystemMediaTransportControls.GetForCurrentView();
            deferral = taskInstance.GetDeferral();

            ActivateSystemMediaTransportControl();
            systemMediaTransportControl.ButtonPressed += MediaTransportControlButtonPressed;

            SetEvents(taskInstance);

            LoadCurrentSongAndLibrary();
        }

        private void SetEvents(IBackgroundTaskInstance taskInstance)
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded += BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened += BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed += BackgroundMediaPlayer_MediaFailed;

            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += Taskcompleted;
        }

        private async void LoadCurrentSongAndLibrary()
        {/*
            if (lastPressedButton == SystemMediaTransportControlsButton.Play)
            {
                await LibraryLib.CurrentSong.Current.Load();
                Play();
                await LoadLibraryData();
                return;
            }               //      */

            await LoadLibraryData();

            SetCurrentSong(false);
            /*
            if (lastPressedButton == defaultPressedButton) lastPressedButton = SystemMediaTransportControlsButton.Pause;
            else if (lastPressedButton != SystemMediaTransportControlsButton.Pause)
            {
                MediaTransportControlButtonPressed(lastPressedButton);
            }
            else Play();         //              */
        }

        private async Task LoadLibraryData()
        {
            await Library.Current.LoadAsync();
            SetLoopToBackgroundPlayer();

            ForegroundCommunicator.SendXmlText();
            //SaveText("Load");
        }

        public void ActivateSystemMediaTransportControl()
        {
            systemMediaTransportControl.IsEnabled = 
                systemMediaTransportControl.IsPauseEnabled = systemMediaTransportControl.IsPlayEnabled = 
                //systemMediaTransportControl.IsRewindEnabled = systemMediaTransportControl.IsFastForwardEnabled = 
                systemMediaTransportControl.IsPreviousEnabled = systemMediaTransportControl.IsNextEnabled = true;
        }

        public void SetLoopToBackgroundPlayer()
        {
            BackgroundMediaPlayer.Current.IsLoopingEnabled = CurrentPlaylist.Loop == LoopKind.Current;
        }

        public void Play()
        {
            autoPlay = true;

            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing) ;
            else if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Closed ||
                BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped) SetCurrentSong(true);
            else if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing)
            {
                BackgroundMediaPlayer.Current.Volume = 0;
                BackgroundMediaPlayer.Current.Play();

                Volume0To1();
            }

            systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
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
            systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Paused;

            ForegroundCommunicator.SendPause();

            DeaktivateSystemMediaTransportControlButtons();
            LibraryLib.CurrentSong.Current.Save();
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
            if (CurrentSong.IsEmptyOrLoading || CurrentSong == openSong) return;

            StorageFile file;
            this.autoPlay = autoPlay;
            pauseAllowed = !autoPlay;
            BackgroundMediaPlayer.Current.AutoPlay = false;

            try
            {
                file = CurrentSong.GetStorageFile();
                BackgroundMediaPlayer.Current.SetFileSource(file);
                FolderMusicUwpLib.SaveTextClass.SaveText("Set",CurrentSong.Title);
            }
            catch
            {
                FolderMusicUwpLib.SaveTextClass.SaveText("Catch", CurrentSong.Title);
                SkipSong(CurrentSong);
                Task.Delay(100).Wait();

                BackgroundMediaPlayer.Current.SetUriSource(null);

                if (playNext) Next(true);
                else Previous();
            }
        }

        private async void SkipSong(Song song)
        {
            await SkipSongs.AddSkipSongAndSave(song);

            ForegroundCommunicator.SendSkip();
        }

        private async void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            FolderMusicUwpLib.SaveTextClass.SaveText("Open", CurrentSong.Title);

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
            else systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Paused;

            playNext = true;

            if (CurrentSong.NaturalDurationMilliseconds == 1)
            {
                CurrentSong.NaturalDurationMilliseconds = sender.NaturalDuration.TotalMilliseconds;
            }

            UpdateSystemMediaTransportControl();
            /*
            if (!Library.IsLoaded) await LoadLibraryData();
            else                //      */
            {
                ForegroundCommunicator.SendSongsIndexAndShuffleIfComplete();
            }

            await LibraryLib.CurrentSong.Current.Save();
        }

        private void UpdateSystemMediaTransportControl()
        {
            systemMediaTransportControl.DisplayUpdater.Type = MediaPlaybackType.Music;

            if (systemMediaTransportControl.DisplayUpdater.MusicProperties.Title != CurrentSong.Title ||
                systemMediaTransportControl.DisplayUpdater.MusicProperties.Artist != CurrentSong.Artist)
            {
                systemMediaTransportControl.DisplayUpdater.MusicProperties.Title = CurrentSong.Title;
                systemMediaTransportControl.DisplayUpdater.MusicProperties.Artist = CurrentSong.Artist;
                systemMediaTransportControl.DisplayUpdater.Update();
            }
        }

        private void BackgroundMediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            FolderMusicUwpLib.SaveTextClass.SaveText("Fail", CurrentSong.Title);
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
            Next(true, true);
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing) Play();
            else if (sender.CurrentState == MediaPlayerState.Paused && pauseAllowed) Pause();
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            //SaveText(4, Library.IsLoaded, args.Button.ToString());
            //lastPressedButton = args.Button;

            MediaTransportControlButtonPressed(args.Button);
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

        public void SetForegroundIsActiv()
        {
            gotReplayFromForeground = true;
        }

        private async void DeaktivateSystemMediaTransportControlButtons()
        {
            FolderMusicUwpLib.SaveTextClass.SaveText("WaitStart");

            deactivateSMTC++;
            await Task.Delay(TimeSpan.FromSeconds(289));

            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing || 
                --deactivateSMTC > 0 || systemMediaTransportControl.IsEnabled == false) return;

            FolderMusicUwpLib.SaveTextClass.SaveText("BeforeQuestion");

            gotReplayFromForeground = false;
            ForegroundCommunicator.SendIsActiv();
            await Task.Delay(1000);

            if (gotReplayFromForeground) return;

            FolderMusicUwpLib.SaveTextClass.SaveText("Deactivate");

            systemMediaTransportControl.IsPreviousEnabled = systemMediaTransportControl.IsNextEnabled =
                //systemMediaTransportControl.IsRewindEnabled = systemMediaTransportControl.IsFastForwardEnabled = 
                systemMediaTransportControl.IsPauseEnabled = false;
        }

        private void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            FolderMusicUwpLib.SaveTextClass.SaveText("Completed");

            LibraryLib.CurrentSong.Current.Save();
            Library.Current.SaveAsync();

            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            FolderMusicUwpLib.SaveTextClass.SaveText("Canceled");

            LibraryLib.CurrentSong.Current.Save();
            Library.Current.SaveAsync();

            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();
        }
    }
}
