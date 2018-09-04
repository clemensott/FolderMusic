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
        private const SystemMediaTransportControlsButton defaultPressedButton = SystemMediaTransportControlsButton.ChannelDown;

        private static BackgroundAudioTask task;
        private BackgroundTaskDeferral deferral;
        private SystemMediaTransportControls systemMediaTransportControl;

        private bool autoPlay = false, pauseAllowed = true, playNext = true, saved;
        private long lastTicks;
        private Song openSong;
        private static SystemMediaTransportControlsButton lastPressedButton = defaultPressedButton;

        private string id;

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
            id = taskInstance.InstanceId.ToString();
            SaveText(Convert.ToUInt32(new Random().Next(10, 1000)), DateTime.Now.Ticks, "Run", id,
                taskInstance.TriggerDetails == null ? "" : taskInstance.TriggerDetails);
            /*SaveText(Convert.ToUInt32(new Random().Next(10, 100)), DateTime.Now.Ticks, lastPressedButton,
                Library.IsLoaded, saved, task == null, lastTicks);      //      */

            task = this;
            saved = false;

            lastTicks = DateTime.Now.Ticks;

            ForegroundCommunicator.SetReceivedEvent();

            systemMediaTransportControl = SystemMediaTransportControls.GetForCurrentView();
            deferral = taskInstance.GetDeferral();

            SetSystemMediaTransportControlDefaultSettings();
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
        {
            if (true/*lastPressedButton == SystemMediaTransportControlsButton.Play*/)
            {
                await LibraryLib.CurrentSong.Current.Load();
                Play();
                await LoadLibraryData();
                return;
            }

            await LoadLibraryData();

            if (lastPressedButton == defaultPressedButton) lastPressedButton = SystemMediaTransportControlsButton.Pause;
            if (lastPressedButton != SystemMediaTransportControlsButton.Pause)
            {
                MediaTransportControlButtonPressed(lastPressedButton);
            }
            else Play();
        }

        private async Task LoadLibraryData()
        {
            await Library.Current.LoadAsync();
            SetLoopToBackgroundPlayer();

            ForegroundCommunicator.SendXmlText();
            SaveText(Convert.ToUInt32(new Random().Next(10, 1000)), DateTime.Now.Ticks,"Load", id);
        }

        private void SetSystemMediaTransportControlDefaultSettings()
        {
            systemMediaTransportControl.IsEnabled = true;
            systemMediaTransportControl.IsPauseEnabled = true;
            systemMediaTransportControl.IsPlayEnabled = true;
            systemMediaTransportControl.IsPreviousEnabled = true;
            systemMediaTransportControl.IsNextEnabled = true;
            systemMediaTransportControl.IsRewindEnabled = true;
            systemMediaTransportControl.IsFastForwardEnabled = true;

            systemMediaTransportControl.ButtonPressed += MediaTransportControlButtonPressed;
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
            else if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing) BackgroundMediaPlayer.Current.Play();

            systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
        }

        public void Pause()
        {
            autoPlay = false;
            BackgroundMediaPlayer.Current.Pause();

            systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Paused;
            ForegroundCommunicator.SendPause();

            LibraryLib.CurrentSong.Current.Save();
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
                SaveText(Convert.ToUInt32(new Random().Next(10, 1000)), DateTime.Now.Ticks, "Set", id);
            }
            catch
            {
                SaveText(Convert.ToUInt32(new Random().Next(10, 1000)), DateTime.Now.Ticks, "Catch", id);
                SkipSongs.AddSkipSongAndSave(CurrentSong);
                Task.Delay(100).Wait();

                BackgroundMediaPlayer.Current.SetUriSource(null);
                ForegroundCommunicator.SendSkip();

                if (playNext) Next(true);
                else Previous();
            }
        }

        private async void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            SaveText(Convert.ToUInt32(new Random().Next(10, 1000)), DateTime.Now.Ticks, "Open", id);

            openSong = CurrentSong;
            sender.Position = TimeSpan.FromMilliseconds(CurrentSongPositionTotalMilliseconds);

            if (autoPlay) sender.Play();
            else systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Paused;

            playNext = true;

            if (CurrentSong.NaturalDurationMilliseconds == 1)
            {
                CurrentSong.NaturalDurationMilliseconds = sender.NaturalDuration.TotalMilliseconds;
            }

            UpdateSystemMediaTransportControl();

            if (!Library.IsLoaded) await LoadLibraryData();
            else ForegroundCommunicator.SendSongsIndexAndShuffleIfComplete();

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

        private async void BackgroundMediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            SaveText(Convert.ToUInt32(new Random().Next(10, 1000)), DateTime.Now.Ticks, "Fail", id);
            Task.Delay(100).Wait();

            if (args.Error == MediaPlayerError.Unknown)
            {
                SetCurrentSong();
                return;
            }

            CurrentSong.SetFailed();
            await SkipSongs.AddSkipSongAndSave(CurrentSong);
            ForegroundCommunicator.SendSkip();

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
            SaveText(4, DateTime.Now.Ticks, Library.IsLoaded, args.Button.ToString(), id);
            lastPressedButton = args.Button;

            int beforeShuffleListIndex = Library.Current.CurrentPlaylist.ShuffleListIndex;

            if (Library.IsLoaded) MediaTransportControlButtonPressed(args.Button);
            else lastPressedButton = args.Button;

            if (!Library.IsLoaded)
            {
                SaveText(1, DateTime.Now.Ticks, Library.IsLoaded, lastPressedButton.ToString());
            }

            long nowTicks = DateTime.Now.Ticks;
            if (nowTicks - lastTicks < 1000000)
            {
                SaveText(2, nowTicks, "DeltaTicks: ", nowTicks - lastTicks);
                lastTicks = nowTicks;
            }

            if (Math.Abs(Library.Current.CurrentPlaylist.ShuffleListIndex - beforeShuffleListIndex) > 1)
            {
                SaveText(3, DateTime.Now.Ticks, "DeltaIndex: ", Library.Current.CurrentPlaylist.ShuffleListIndex - beforeShuffleListIndex);
            }
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControlsButton button)
        {
            switch (button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Play();
                    break;

                case SystemMediaTransportControlsButton.Pause:
                    Pause();
                    break;

                case SystemMediaTransportControlsButton.Previous:
                    Previous();
                    break;

                case SystemMediaTransportControlsButton.Next:
                    Next(IsPlaying);
                    break;
            }
        }

        private void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();

            if (saved) return;

            saved = true;
            LibraryLib.CurrentSong.Current.Save();
            Library.Current.SaveAsync();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();

            if (saved) return;

            saved = true;
            LibraryLib.CurrentSong.Current.Save();
            Library.Current.SaveAsync();
        }

        private async Task SaveText(uint no, params object[] objs)
        {
            try
            {
                string text = "";
                string filename = string.Format("Text{0}.txt", no);
                StorageFile file = await ApplicationData.Current.LocalFolder.
                    CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                foreach (object obj in objs) text += obj.ToString() + ";";

                text = text.TrimEnd(';');

                await PathIO.WriteTextAsync(file.Path, text);
            }
            catch { }
        }
    }
}
