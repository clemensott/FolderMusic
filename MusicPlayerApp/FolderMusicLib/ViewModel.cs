using LibraryLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace FolderMusicLib
{
    public class ViewModel : INotifyPropertyChanged
    {
        private static ViewModel instance;

        private bool playerPositionEnabled = true, mainPageLoaded;
        private SymbolIcon playIcon, pauseIcon;

        public static ViewModel Current
        {
            get
            {
                if (!Library.Current.IsForeground) return null;
                if (instance == null) instance = new ViewModel();

                return instance;
            }
        }

        public bool PlayerPositionEnabled
        {
            get { return playerPositionEnabled; }
            set
            {
                if (value == playerPositionEnabled) return;

                playerPositionEnabled = value;

                if (playerPositionEnabled) BackgroundMediaPlayer.Current.Position = TimeSpan.FromMilliseconds(PlayerPositionMillis);
            }
        }

        public bool IsMainPageLoaded { get { return mainPageLoaded; } }

        public int PlaylistsIndex
        {
            get { return Library.Current.CurrentPlaylistIndex; }
            set { UpdatePlaylistIndex(); }
        }

        public double PlayerPostionPercent
        {
            get { return CurrentPlaylist.SongPositionPercent; }
            set
            {
                if (value > 0 && value < 1) { }

                if (value == PlayerPostionPercent && Math.Abs(value * PlayerDurationMillis - PlayerPositionMillis) < 100) return;

                CurrentPlaylist.SongPositionPercent = value;
                UpdatePlayerPositionAndDuration();

                if (playerPositionEnabled) BackgroundMediaPlayer.Current.Position = TimeSpan.FromMilliseconds(PlayerPositionMillis);
            }
        }

        public double PlayerPositionMillis
        {
            get { return PlayerPostionPercent * PlayerDurationMillis; }
        }

        public double PlayerDurationMillis
        {
            get { return CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds; }
            set { CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds = value; }
        }

        public string PlayerPositionText { get { return GetShowTime(PlayerPositionMillis); } }

        public string PlayerDurationText
        {
            get { return GetShowTime(PlayerDurationMillis); }
        }

        public string PlayPauseText
        {
            get
            {
                return BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing ? "Pause" : "Play";
            }
        }

        public IconElement PlayPauseIcon
        {
            get
            {
                return BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing ? GetPauseIcon() : GetPlayIcon();
            }
        }

        public Playlist CurrentPlaylist { get { return Library.Current.CurrentPlaylist; } }

        public List<Playlist> Playlists { get { return Library.Current.Playlists; } }

        private ViewModel() { }

        private SymbolIcon GetPlayIcon()
        {
            try
            {
                if (playIcon == null) playIcon = new SymbolIcon(Symbol.Play);
            }
            catch
            {
                return new SymbolIcon(Symbol.Play);
            }

            return playIcon;
        }

        private SymbolIcon GetPauseIcon()
        {
            try
            {
                if (pauseIcon == null) pauseIcon = new SymbolIcon(Symbol.Pause);
            }
            catch
            {
                return new SymbolIcon(Symbol.Pause);
            }

            return pauseIcon;
        }

        private string GetShowTime(double totalMilliseconds)
        {
            int totalSeconds = Convert.ToInt32(totalMilliseconds / 1000);
            int seconds = totalSeconds % 60, minutes = (totalSeconds / 60) % 60, hours = totalSeconds / 3600;
            string time = "";

            time += hours > 0 ? hours.ToString() + ":" : "";
            time += hours > 0 ? string.Format("{0,2}", minutes) : minutes.ToString();
            time += string.Format(":{0,2}", seconds);

            return time.Replace(" ", "0");
        }

        public void SetMainPageLoaded()
        {
            mainPageLoaded = Library.Current.IsForeground;
        }

        public void Play()
        {
            if (Library.Current.IsForeground) BackgroundCommunicator.SendPlay();
        }

        public void Pause()
        {
            if (Library.Current.IsForeground) BackgroundCommunicator.SendPause();
        }

        public void UpdatePlayerPositionAndDuration()
        {
            UpdatePlayerPosition();
            UpdatePlayerDurationText();
        }

        private void UpdatePlayerPosition()
        {
            NotifyPropertyChanged("PlayerPostionPercent");
            UpdatePlayerPositionText();
        }

        private void UpdatePlayerPositionText()
        {
            NotifyPropertyChanged("PlayerPositionText");
        }

        private void UpdatePlayerDurationText()
        {
            NotifyPropertyChanged("PlayerDurationText");
        }

        public void UpdatePlayPauseIconAndText()
        {
            NotifyPropertyChanged("PlayPauseIcon");
            NotifyPropertyChanged("PlayPauseText");
        }

        public void UpdatePlaylists()
        {
            NotifyPropertyChanged("Playlists");
        }

        public void UpdatePlaylistIndex()
        {
            NotifyPropertyChanged("PlaylistsIndex");
        }

        public void UpdateCurrentPlaylistIndexAndRest()
        {
            UpdatePlaylistIndex();

            NotifyPropertyChanged("CurrentPlaylist");

            CurrentPlaylist.UpdateName();
            CurrentPlaylist.UpdateSongsAndShuffleListSongs();
            CurrentPlaylist.UpdateCurrentSong();

            CurrentPlaylist.UpdateLoopIcon();
            CurrentPlaylist.UpdateShuffleIcon();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                if (null == PropertyChanged) return;

                await Windows.ApplicationModel.Core.CoreApplication.MainView.
                    CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
            }
            catch { }
        }
    }
}
