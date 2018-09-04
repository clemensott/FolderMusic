using MusicPlayer;
using MusicPlayer.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;

namespace BackgroundTask
{
    class MusicPlayer : IBackgroundPlayer
    {
        private const int maxFailOrSetCount = 5;

        private bool playNext = true;
        private int failedCount = 0, setSongCount = 0;
        private Song openSong;
        private ILibrary library;
        private BackgroundAudioTask task;
        private SystemMediaTransportControls smtc;

        private Song CurrentSong { get { return library.CurrentPlaylist?.CurrentSong; } }

        private IPlaylist CurrentPlaylist { get { return library.CurrentPlaylist; } }

        public MusicPlayer(BackgroundAudioTask backgroundAudioTask, SystemMediaTransportControls smtControls, ILibrary library)
        {
            task = backgroundAudioTask;
            smtc = smtControls;
            this.library = library;

            ActivateSystemMediaTransportControl();
        }

        public void ActivateSystemMediaTransportControl()
        {
            smtc.IsEnabled = smtc.IsPauseEnabled = smtc.IsPlayEnabled =
                //smtc.IsRewindEnabled = smtc.IsFastForwardEnabled = 
                smtc.IsPreviousEnabled = smtc.IsNextEnabled = true;
        }

        public void Play()
        {
            if (setSongCount >= maxFailOrSetCount)
            {
                setSongCount = 0;
                BackgroundMediaPlayer.Current.Volume = 1;
                BackgroundMediaPlayer.Current.Play();
                MobileDebug.Manager.WriteEvent("PlayBecauseOfSetSongCount", BackgroundMediaPlayer.Current.CurrentState);
            }
            else if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Closed ||
                 BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped)
            {
                MobileDebug.Manager.WriteEvent("SetOnPlayClosedAndStopped", "SetCount: " + setSongCount,
                    library.CurrentPlaylist?.CurrentSong.ToString() ?? "CurrentSong",
                    openSong.ToString() ?? "OpenSong");
                SetCurrent();
            }
            else if (BackgroundMediaPlayer.Current.NaturalDuration.Ticks == 0)
            {
                MobileDebug.Manager.WriteEvent("SetOnPlayDurationZero", library.CurrentPlaylist?.CurrentSong);
                SetCurrent();
            }
            else if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing)
            {
                double percent = library?.CurrentPlaylist?.CurrentSongPositionPercent ?? -1;
                double duration = library?.CurrentPlaylist?.CurrentSong?.DurationMilliseconds ?? Song.DefaultDuration;

                MobileDebug.Manager.WriteEvent("PlayNormal", BackgroundMediaPlayer.Current.CurrentState, CurrentSong, percent);
                if (percent >= 0) BackgroundMediaPlayer.Current.Position = TimeSpan.FromMilliseconds(percent * duration);

                BackgroundMediaPlayer.Current.Volume = 0;
                BackgroundMediaPlayer.Current.Play();

                Volume0To1();

                setSongCount = 0;
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
            try
            {
                TimeSpan position = BackgroundMediaPlayer.Current.Position;
                TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;
                library.CurrentPlaylist.CurrentSongPositionPercent = position.TotalMilliseconds / duration.TotalMilliseconds;

                smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

                if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Paused)
                {
                    Volume1To0AndPause();
                }
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("MusicPauseFail", e);
            }
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
            CurrentPlaylist.SetNextSong();
            playNext = true;
            bool isLast = CurrentPlaylist.CurrentSong == CurrentPlaylist.Songs.Last();

            if (isLast && fromEnded) library.IsPlaying = false;
        }

        public void Previous()
        {
            playNext = false;
            CurrentPlaylist.SetPreviousSong();
        }

        public void SetCurrent()
        {
            MobileDebug.Manager.WriteEvent("TrySet", "CurSongEmpty: " + (CurrentSong?.IsEmpty.ToString() ?? "null"),
                openSong?.Path ?? "OpenPath", "IsOpen: " + (CurrentSong == openSong), CurrentSong);

            if ((CurrentSong?.IsEmpty ?? true) || (CurrentSong?.Failed ?? true)) return;

            BackgroundMediaPlayer.Current.AutoPlay = library.IsPlaying && CurrentPlaylist.CurrentSongPositionPercent == 0;

            try
            {
                StorageFile file = CurrentSong.GetStorageFile();
                BackgroundMediaPlayer.Current.SetFileSource(file);
                setSongCount++;
                MobileDebug.Manager.WriteEvent("Set", setSongCount, CurrentSong);
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("Catch", e, CurrentSong);
                //library.SkippedSongs.Add(CurrentSong);
                Task.Delay(100).Wait();

                BackgroundMediaPlayer.Current.SetUriSource(null);

                if (playNext) Next(false);
                else Previous();
            }
        }

        public void MediaOpened(MediaPlayer sender, object args)
        {
            MobileDebug.Manager.WriteEvent("Open", setSongCount,
                "Sender.State: " + sender.CurrentState, "IsPlayling: " + library.IsPlaying,
                "Pos: " + CurrentPlaylist.GetCurrentSongPosition().TotalSeconds, CurrentSong);

            playNext = true;
            failedCount = 0;

            if (CurrentSong != openSong)
            {
                Unsubscribe(openSong);
                openSong = CurrentSong;
                Subscribe(CurrentSong);
            }

            if (sender.NaturalDuration.TotalMilliseconds > Song.DefaultDuration)
            {
                CurrentSong.DurationMilliseconds = sender.NaturalDuration.TotalMilliseconds;
            }

            sender.Position = CurrentPlaylist.GetCurrentSongPosition();

            if (library.IsPlaying)
            {
                if (CurrentPlaylist.CurrentSongPositionPercent > 0) Play();
                else setSongCount = 0;
            }
            else smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

            UpdateSystemMediaTransportControl();
        }

        private void Subscribe(Song song)
        {
            if (openSong == null) return;

            song.ArtistChanged += OnCurrentSongArtistOrTitleChanged;
            song.TitleChanged += OnCurrentSongArtistOrTitleChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (openSong == null) return;

            song.ArtistChanged -= OnCurrentSongArtistOrTitleChanged;
            song.TitleChanged -= OnCurrentSongArtistOrTitleChanged;
        }

        private void OnCurrentSongArtistOrTitleChanged(Song sender, EventArgs args)
        {
            UpdateSystemMediaTransportControl();
        }

        private void UpdateSystemMediaTransportControl()
        {
            var du = smtc.DisplayUpdater;

            if (du.Type != MediaPlaybackType.Music) du.Type = MediaPlaybackType.Music;
            if (du.MusicProperties.Title != CurrentSong.Title || du.MusicProperties.Artist != CurrentSong.Artist)
            {
                du.MusicProperties.Title = CurrentSong.Title;
                du.MusicProperties.Artist = CurrentSong.Artist;
                du.Update();
            }
        }

        public void MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            MobileDebug.Manager.WriteEvent("Fail", args.ExtendedErrorCode, args.Error, args.ErrorMessage, CurrentSong);
            Task.Delay(100).Wait();

            failedCount++;

            if (failedCount >= maxFailOrSetCount) failedCount = 0;
            else if (args.Error == MediaPlayerError.Unknown)
            {
                SetCurrent();
                return;
            }

            CurrentSong.SetFailed();
            library.SkippedSongs.Add(CurrentSong);

            if (playNext) Next(true);
            else Previous();
        }

        public void MediaEnded(MediaPlayer sender, object args)
        {
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;

            MobileDebug.Manager.WriteEvent("MusicEnded", "SMTC-State: " + smtc.PlaybackStatus, CurrentSong);

            Next(true);
        }

        public void Dispose()
        {
        }
    }
}
