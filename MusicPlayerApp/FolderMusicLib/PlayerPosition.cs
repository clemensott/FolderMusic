using LibraryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace FolderMusicLib
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
            if (sender.CurrentState == MediaPlayerState.Playing) StartTimer();
            else StopTimer();

            ViewModel.Current.UpdatePlayPauseIconAndText();
        }

        private void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            
        }

        public void StartTimer()
        {
            int timeOut = intervall - Convert.ToInt32(BackgroundMediaPlayer.Current.Position.TotalMilliseconds % intervall);
            timer.Change(timeOut, intervall);

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

            if (position > 1 && duration > 1) Library.Current.CurrentPlaylist.SongPositionPercent = position / duration;
            else
            {
                playlist = Library.Current.CurrentPlaylist;

                position = playlist.SongPositionPercent * playlist.CurrentSong.NaturalDurationMilliseconds;
                position += (currentDateTime - previousUpdatedTime).TotalMilliseconds;

                playlist.SongPositionPercent = position / playlist.CurrentSong.NaturalDurationMilliseconds;
            }

            previousUpdatedTime = currentDateTime;

            ViewModel.Current.UpdatePlayerPositionAndDuration();
        }
    }
}
