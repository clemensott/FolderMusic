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
        private const int maxFailOrSetCount = 15;

        private bool playNext = true;
        private int failedCount = 0, setSongCount = 0;
        private Song openSong;
        private ILibrary library;
        private SystemMediaTransportControls smtc;

        private Song CurrentSong { get { return library.CurrentPlaylist?.CurrentSong; } }

        private IPlaylist CurrentPlaylist { get { return library.CurrentPlaylist; } }

        public MusicPlayer(SystemMediaTransportControls smtControls, ILibrary library)
        {
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
                BackgroundMediaPlayer.Current.Volume = 0;
                BackgroundMediaPlayer.Current.Play();

                Volume0To1();

                MobileDebug.Service.WriteEvent("PlayBecauseOfSetSongCount", BackgroundMediaPlayer.Current.CurrentState);
            }
            else if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Closed ||
                 BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped)
            {
                MobileDebug.Service.WriteEventPair("SetOnPlayClosedAndStopped", "SetCount: ", setSongCount,
                    "CurrentSong: ", library.CurrentPlaylist?.CurrentSong, "OpenSong: ", openSong);
                SetCurrent();
            }
            else if (BackgroundMediaPlayer.Current.NaturalDuration.Ticks == 0)
            {
                MobileDebug.Service.WriteEvent("SetOnPlayDurationZero", library.CurrentPlaylist?.CurrentSong);
                SetCurrent();
            }
            else if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing)
            {
                double percent = library?.CurrentPlaylist?.CurrentSongPositionPercent ?? -1;
                double duration = library?.CurrentPlaylist?.CurrentSong?.DurationMilliseconds ?? Song.DefaultDuration;

                MobileDebug.Service.WriteEvent("PlayNormal", BackgroundMediaPlayer.Current.CurrentState, CurrentSong, percent);
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
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("MusicPauseFail", e);
            }

            smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

            if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Paused) Volume1To0AndPause();
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
            MobileDebug.Service.WriteEventPair("TrySet", "OpenPath: ", openSong?.Path,
                "CurSongEmpty: ", CurrentSong?.IsEmpty, "CurSongFailed: ", CurrentSong?.Failed,
                "IsOpen: ", CurrentSong == openSong, "CurrentSong: ", CurrentSong);

            if ((CurrentSong?.IsEmpty ?? true) || (CurrentSong?.Failed ?? true)) return;

            BackgroundMediaPlayer.Current.AutoPlay = library.IsPlaying && CurrentPlaylist.CurrentSongPositionPercent == 0;

            try
            {
                StorageFile file = CurrentSong.GetStorageFile();
                BackgroundMediaPlayer.Current.SetFileSource(file);
                setSongCount++;
                MobileDebug.Service.WriteEvent("Set", setSongCount, CurrentSong);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("Catch", e, CurrentSong);
                library.SkippedSongs.Add(CurrentSong);
                Task.Delay(100).Wait();

                if (playNext) Next(false);
                else Previous();
            }
        }

        public void MediaOpened(MediaPlayer sender, object args)
        {
            MobileDebug.Service.WriteEventPair("Open", "SetSongCount", setSongCount, "Sender.State: ", sender.CurrentState,
                "IsPlayling: ", library.IsPlaying, "Pos: ", CurrentPlaylist.GetCurrentSongPosition().TotalSeconds,
                "CurrentSong: ", CurrentSong);

            playNext = true;
            failedCount = 0;

            if (CurrentSong != openSong)
            {
                Unsubscribe(openSong);
                openSong = CurrentSong;
                Subscribe(CurrentSong);
            }

            if (library.IsLoadedComplete && sender.NaturalDuration.TotalMilliseconds > Song.DefaultDuration)
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

        public void UpdateSystemMediaTransportControl()
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
            MobileDebug.Service.WriteEvent("Fail", args.ExtendedErrorCode, args.Error, args.ErrorMessage, CurrentSong);
            Task.Delay(100).Wait();

            failedCount++;

            if (failedCount >= maxFailOrSetCount) failedCount = 0;
            else if (args.Error == MediaPlayerError.Unknown)
            {
                Task.Delay(2000).Wait();

                SetCurrent();
                return;
            }

            //if (args.ExtendedErrorCode.Message == "")
            //{
            //    library.IsPlaying = false;
            //    return;
            //}4

            CurrentSong.SetFailed();
            library.SkippedSongs.Add(CurrentSong);

            if (playNext) Next(true);
            else Previous();
        }

        public void MediaEnded(MediaPlayer sender, object args)
        {
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;

            MobileDebug.Service.WriteEventPair("MusicEnded", "SMTC-State: ", smtc.PlaybackStatus,
                "CurrentSong: ", CurrentSong);

            Next(true);
        }

        public void Dispose()
        {
        }
    }
}
