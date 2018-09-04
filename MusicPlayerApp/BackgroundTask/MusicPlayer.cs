using MusicPlayer.Data;
using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;

namespace BackgroundTask
{
    class MusicPlayer : IBackgroundPlayer
    {
        private bool playNext = true;
        private int failedCount = 0;
        private bool isOpening;
        private Song openSong;
        private BackgroundAudioTask task;
        private SystemMediaTransportControls smtc;

        private Song CurrentSong { get { return Library.Current.CurrentPlaylist.CurrentSong; } }

        private Playlist CurrentPlaylist { get { return Library.Current.CurrentPlaylist; } }

        public MusicPlayer(BackgroundAudioTask backgroundAudioTask, SystemMediaTransportControls smtControls)
        {
            task = backgroundAudioTask;
            smtc = smtControls;

            ActivateSystemMediaTransportControl();

            Feedback.Current.OnTitlePropertyChanged += OnTitlePropertyChanged;
            Feedback.Current.OnArtistPropertyChanged += OnArtistPropertyChanged;
        }

        public void ActivateSystemMediaTransportControl()
        {
            smtc.IsEnabled = smtc.IsPauseEnabled = smtc.IsPlayEnabled =
                //smtc.IsRewindEnabled = smtc.IsFastForwardEnabled = 
                smtc.IsPreviousEnabled = smtc.IsNextEnabled = true;
        }

        public void Play()
        {
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Closed ||
                BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped)
            {
                FolderMusicDebug.DebugEvent.SaveText("SetOnPlayClosedAndStopped", Library.Current.CurrentPlaylist.CurrentSong);
                SetCurrent();
            }
            else if (BackgroundMediaPlayer.Current.NaturalDuration.Ticks == 0)
            {
                openSong = null;
                FolderMusicDebug.DebugEvent.SaveText("SetOnPlayDurationZero", Library.Current.CurrentPlaylist.CurrentSong);
                SetCurrent();
            }
            else if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing)
            {
                BackgroundMediaPlayer.Current.Volume = 0;
                BackgroundMediaPlayer.Current.Play();

                Volume0To1();
            }

            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
        }

        private void Volume0To1()
        {
            double step = 0.1;

            for (double i = step; i < 1; i += step)
            {
                BackgroundMediaPlayer.Current.Volume = Math.Sqrt(i);
                Task.Delay(10).Wait();
            }
        }

        public void Pause()
        {
            TimeSpan position = BackgroundMediaPlayer.Current.Position;
            TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;
            Library.Current.CurrentPlaylist.SongPositionPercent = position.TotalMilliseconds / duration.TotalMilliseconds;

            if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Paused)
            {
                Volume1To0AndPause();
            }

            smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

            CurrentPlaySong.Current.SaveAsync();
        }

        private void Volume1To0AndPause()
        {
            double step = 0.1;

            for (double i = 1; i > 0; i -= step)
            {
                BackgroundMediaPlayer.Current.Volume = Math.Sqrt(i);
                Task.Delay(1).Wait();
            }

            BackgroundMediaPlayer.Current.Pause();
        }

        public void Next(bool fromEnded)
        {
            playNext = true;
            bool wasLast = CurrentPlaylist.SetNextSong();

            if (wasLast && fromEnded) Library.Current.IsPlaying = false;

            //if (!CurrentSong.Failed) SetCurrent();
        }

        public void Previous()
        {
            playNext = false;
            CurrentPlaylist.SetPreviousSong();

            //if (!CurrentSong.Failed) SetCurrent();
        }

        public void SetCurrent()
        {
            if (CurrentSong.IsEmptyOrLoading || CurrentSong == openSong) return;

            BackgroundMediaPlayer.Current.AutoPlay = false;

            try
            {
                StorageFile file = CurrentSong.GetStorageFile();
                BackgroundMediaPlayer.Current.SetFileSource(file);
                FolderMusicDebug.DebugEvent.SaveText("Set", CurrentSong);
            }
            catch
            {
                FolderMusicDebug.DebugEvent.SaveText("Catch", CurrentSong);
                //Library.Current.SkippedSongs.Add(CurrentSong);
                Task.Delay(100).Wait();

                BackgroundMediaPlayer.Current.SetUriSource(null);

                if (playNext) Next(false);
                else Previous();
            }
        }

        public void MediaOpened(MediaPlayer sender, object args)
        {
            FolderMusicDebug.DebugEvent.SaveText("Open", CurrentSong);

            playNext = true;
            failedCount = 0;
            openSong = CurrentSong;

            if (sender.NaturalDuration.TotalMilliseconds > Song.DefaultDuration)
            {
                CurrentSong.NaturalDurationMilliseconds = sender.NaturalDuration.TotalMilliseconds;
            }

            sender.Position = TimeSpan.FromMilliseconds(CurrentPlaylist.SongPositionMillis);

            if (Library.Current.IsPlaying)
            {
                if (sender.Position.TotalMilliseconds == Playlist.DefaultSongsPositionMillis)
                {
                    sender.Volume = 1;
                    sender.Play();
                }
                else Play();
            }
            else smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

            UpdateSystemMediaTransportControl();

            if (!Library.IsLoaded()) task.LoadLibraryData();

            CurrentPlaySong.Current.SaveAsync();
        }

        private TimeSpan GetSongPosition(MediaPlayer player)
        {
            if (player.NaturalDuration.TotalSeconds < 1) return TimeSpan.FromMilliseconds(CurrentPlaylist.SongPositionMillis);

            return TimeSpan.FromMilliseconds(CurrentPlaylist.SongPositionPercent * player.NaturalDuration.TotalMilliseconds);
        }

        private void UpdateSystemMediaTransportControl()
        {
            if (smtc.DisplayUpdater.Type != MediaPlaybackType.Music) smtc.DisplayUpdater.Type = MediaPlaybackType.Music;

            if (smtc.DisplayUpdater.MusicProperties.Title != CurrentSong.Title ||
                smtc.DisplayUpdater.MusicProperties.Artist != CurrentSong.Artist)
            {
                smtc.DisplayUpdater.MusicProperties.Title = CurrentSong.Title;
                smtc.DisplayUpdater.MusicProperties.Artist = CurrentSong.Artist;
                smtc.DisplayUpdater.Update();
            }
        }

        public void MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            FolderMusicDebug.DebugEvent.SaveText("Fail", args.ErrorMessage, CurrentSong);
            Task.Delay(100).Wait();

            failedCount++;

            if (args.Error == MediaPlayerError.Unknown)
            {
                SetCurrent();
                return;
            }

            CurrentSong.SetFailed();
            Library.Current.SkippedSongs.Add(CurrentSong);

            if (playNext) Next(true);
            else Previous();
        }

        public void MediaEnded(MediaPlayer sender, object args)
        {
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;

            FolderMusicDebug.DebugEvent.SaveText("MusicEnded", "SMTC-State: " + smtc.PlaybackStatus, CurrentSong);

            Next(true);
        }

        private void OnTitlePropertyChanged(Song sender, SongTitleChangedEventArgs args)
        {
            if (openSong?.Path == sender?.Path) UpdateSystemMediaTransportControl();
        }

        private void OnArtistPropertyChanged(Song sender, SongArtistChangedEventArgs args)
        {
            if (openSong.Path == sender.Path) UpdateSystemMediaTransportControl();
        }

        public void Dispose()
        {
            Feedback.Current.OnTitlePropertyChanged -= OnTitlePropertyChanged;
            Feedback.Current.OnArtistPropertyChanged -= OnArtistPropertyChanged;
        }
    }
}
