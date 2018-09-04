using MusicPlayer.Data;
using System;
using System.Threading;
using Windows.Media.Playback;

namespace FolderMusic
{
    public class PlayerPosition
    {
        private const int intervall = 1000;

        private DateTime previousUpdatedTime;
        private Timer timer;

        public double BackgroundPlayerPositionMilliseconds
        {
            get { return BackgroundMediaPlayer.Current.Position.TotalMilliseconds; }
        }

        public double BackgroundPlayerNaturalDurationMilliseconds
        {
            get { return BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds; }
        }

        public PlayerPosition()
        {
            timer = new Timer(new TimerCallback(UpdateSongPosition), new object(), Timeout.Infinite, intervall);

            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaOpened += BackgroundMediaPlayer_MediaOpened;
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            try
            {
                if (sender.CurrentState == MediaPlayerState.Playing) StartTimer();
                else StopTimer();

                ViewModel.Current.UpdatePlayPauseIconAndText();
            }
            catch { }
        }

        private void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            
        }

        public void StartTimer()
        {
            int timeOut = intervall - Convert.ToInt32(BackgroundMediaPlayer.Current.Position.TotalMilliseconds % intervall);
            timer.Change(timeOut, intervall);

            previousUpdatedTime = DateTime.Now;
            UpdateSongPosition(null);
        }

        public void StopTimer()
        {
            timer.Change(Timeout.Infinite, intervall);
        }

        private void UpdateSongPosition(object state)
        {
            if (!ViewModel.Current.PlayerPositionEnabled) return;

            Playlist playlist;
            DateTime currentDateTime = DateTime.Now;
            double position = BackgroundPlayerPositionMilliseconds;
            double duration = BackgroundPlayerNaturalDurationMilliseconds;

            Library.Current.CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds = duration;

            if (position > 1000 && duration > 1000) Library.Current.CurrentPlaylist.SongPositionPercent = position / duration;
            else
            {
                playlist = Library.Current.CurrentPlaylist;

                position = playlist.SongPositionMillis;
                position += (currentDateTime - previousUpdatedTime).TotalMilliseconds;

                playlist.SongPositionPercent = position / playlist.CurrentSong.NaturalDurationMilliseconds;
            }

            previousUpdatedTime = currentDateTime;

           // ViewModel.Current.UpdatePlayerPositionAndDuration();
        }
    }
}
