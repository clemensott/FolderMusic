using MusicPlayer.Data;
using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using PlayerIcons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace FolderMusic
{
    public class ViewModel : INotifyPropertyChanged
    {
        private static ViewModel instance;

        private bool playerPositionEnabled = true;
        private SymbolIcon playIcon, pauseIcon;

        public static ViewModel Current
        {
            get
            {
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

        public int PlaylistsIndex
        {
            get { return Library.Current.CurrentPlaylistIndex; }
            set { UpdatePlaylistIndex(); }
        }

        public double PlayerPositionPercent
        {
            get { return CurrentPlaylist.SongPositionPercent; }
            set
            {
                if (value > 0 && value < 1) { }

                if (value == PlayerPositionPercent && Math.Abs(value * PlayerDurationMillis - PlayerPositionMillis) < 100) return;

                CurrentPlaylist.SongPositionPercent = value;
                UpdatePlayerPositionAndDuration();

                if (playerPositionEnabled) BackgroundMediaPlayer.Current.Position = TimeSpan.FromMilliseconds(PlayerPositionMillis);
            }
        }

        public double PlayerPositionMillis
        {
            get { return PlayerPositionPercent * PlayerDurationMillis; }
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
                return Library.Current.IsPlaying ? GetPauseIcon() : GetPlayIcon();
            }
        }

        public Playlist CurrentPlaylist { get { return Library.Current.CurrentPlaylist; } }

        public List<Playlist> Playlists { get { return Library.Current.Playlists.ToList(); } }

        public string CurrentPlaylistName { get { return CurrentPlaylist.Name; } }

        public BitmapImage CurrentPlaylistShuffleIcon { get { return GetCurrentPlaylistShuffleIcon(); } }

        public BitmapImage CurrentPlaylistLoopIcon { get { return GetCurrentPlaylistLoopIcon(); } }

        public string CurrentSongTitle { get { return CurrentPlaylist.CurrentSong.Title; } }

        public string CurrentSongArtist { get { return CurrentPlaylist.CurrentSong.Artist; } }

        private ViewModel()
        {
            Feedback.Current.OnArtistPropertyChanged += OnArtistPropertyChanged;
            Feedback.Current.OnCurrentPlaylistPropertyChanged += OnCurrentPlaylistPropertyChanged;
            Feedback.Current.OnCurrentSongPositionPropertyChanged += OnCurrentSongPositionPropertyChanged;
            Feedback.Current.OnCurrentSongPropertyChanged += OnCurrentSongPropertyChanged;
            Feedback.Current.OnLibraryChanged += OnLibraryChanged;
            Feedback.Current.OnLoopPropertyChanged += OnLoopPropertyChanged;
            Feedback.Current.OnNaturalDurationPropertyChanged += OnNaturalDurationPropertyChanged;
            Feedback.Current.OnPlayStateChanged += OnPlayStateChanged;
            Feedback.Current.OnPlaylistsPropertyChanged += OnPlaylistsPropertyChanged;
            Feedback.Current.OnShufflePropertyChanged += OnShufflePropertyChanged;
            Feedback.Current.OnSkippedSongsPropertyChanged += OnSkippedSongsPropertyChanged;
            Feedback.Current.OnSongsPropertyChanged += OnSongsPropertyChanged;
            Feedback.Current.OnTitlePropertyChanged += OnTitlePropertyChanged;

            Library.Load(true);
        }

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
            try
            {
                int totalSeconds = Convert.ToInt32(totalMilliseconds / 1000);
                int seconds = totalSeconds % 60, minutes = (totalSeconds / 60) % 60, hours = totalSeconds / 3600;
                string time = string.Empty;

                time += hours > 0 ? hours.ToString() + ":" : string.Empty;
                time += hours > 0 ? string.Format("{0,2}", minutes) : minutes.ToString();
                time += string.Format(":{0,2}", seconds);

                return time.Replace(" ", "0");
            }
            catch { }

            return "Catch";
        }

        private BitmapImage GetCurrentPlaylistShuffleIcon()
        {
            switch (CurrentPlaylist.Shuffle)
            {
                case ShuffleType.Complete:
                    return Icons.Current.ShuffleComplete;

                case ShuffleType.OneTime:
                    return Icons.Current.ShuffleOneTime;

                default:
                    return Icons.Current.ShuffleOff;
            }
        }

        private BitmapImage GetCurrentPlaylistLoopIcon()
        {
            switch (CurrentPlaylist.Loop)
            {
                case LoopType.All:
                    return Icons.Current.LoopAll;

                case LoopType.Current:
                    return Icons.Current.LoopCurrent;

                default:
                    return Icons.Current.LoopOff;
            }
        }

        public void UpdatePlayerPositionAndDuration()
        {
            UpdatePlayerPosition();
            UpdatePlayerDurationText();
        }

        private void UpdatePlayerPosition()
        {
            NotifyPropertyChanged("PlayerPositionPercent");
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

            NotifyPropertyChanged("CurrentPlaylistName");
            NotifyPropertyChanged("CurrentPlaylistLoop");
            NotifyPropertyChanged("CurrentPlaylistShuffleIcon");

            UpdateCurrentSongTitleAndArtist();
        }

        public void UpdateCurrentSongTitleAndArtist()
        {
            NotifyPropertyChanged("CurrentSongTitle");
            NotifyPropertyChanged("CurrentSongArtist");
        }

        private void OnLibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            UpdatePlayPauseIconAndText();
            UpdatePlayerPositionAndDuration();
            UpdatePlaylists();
            UpdateCurrentPlaylistIndexAndRest();
            UpdateCurrentSongTitleAndArtist();
        }

        private void OnPlaylistsPropertyChanged(ILibrary sender, PlaylistsChangedEventArgs args)
        {
            UpdatePlaylists();
            UpdateCurrentPlaylistIndexAndRest();
        }

        private void OnCurrentPlaylistPropertyChanged(ILibrary sender, CurrentPlaylistChangedEventArgs args)
        {
            UpdateCurrentPlaylistIndexAndRest();
        }

        private void OnSkippedSongsPropertyChanged(SkipSongs sender)
        {
            if (sender.MoveNext()) (Window.Current.Content as Frame).Navigate(typeof(SkipSongsPage));
        }

        private void OnSongsPropertyChanged(Playlist sender, SongsChangedEventArgs args)
        {
            if (sender == CurrentPlaylist) NotifyPropertyChanged("CurrentPlaylistShuffleIcon");
            if (args.NewShuffleList.Count != args.OldShuffleList.Count) UpdatePlaylists();
        }

        private void OnCurrentSongPropertyChanged(Playlist sender, CurrentSongChangedEventArgs args)
        {
            if (sender != CurrentPlaylist) return;

            UpdateCurrentSongTitleAndArtist();
            UpdatePlayerPositionAndDuration();
        }

        private void OnCurrentSongPositionPropertyChanged(Playlist sender, CurrentSongPositionChangedEventArgs args)
        {
            if (sender == CurrentPlaylist) UpdatePlayerPositionAndDuration();
        }

        private void OnShufflePropertyChanged(Playlist sender, ShuffleChangedEventArgs args)
        {
            if (sender == CurrentPlaylist) NotifyPropertyChanged("CurrentPlaylistShuffleIcon");
        }

        private void OnLoopPropertyChanged(Playlist sender, LoopChangedEventArgs args)
        {
            if (sender == CurrentPlaylist) NotifyPropertyChanged("CurrentPlaylistLoopIcon");
        }

        private void OnTitlePropertyChanged(Song sender, SongTitleChangedEventArgs args)
        {
            if (sender == CurrentPlaylist.CurrentSong) NotifyPropertyChanged("CurrentSongTitle");
            else if (CurrentPlaylist.Songs.Contains(sender)) NotifyPropertyChanged("CurrentPlaylistShuffleIcon");
        }

        private void OnArtistPropertyChanged(Song sender, SongArtistChangedEventArgs args)
        {
            if (sender == CurrentPlaylist.CurrentSong) NotifyPropertyChanged("CurrentSongArtist");
            else if (CurrentPlaylist.Songs.Contains(sender)) NotifyPropertyChanged("CurrentPlaylistShuffleIcon");
        }

        private void OnNaturalDurationPropertyChanged(Song sender, SongNaturalDurationChangedEventArgs args)
        {
            if (sender == CurrentPlaylist.CurrentSong) UpdatePlayerPositionAndDuration();
        }

        private void OnPlayStateChanged(ILibrary sender, PlayStateChangedEventArgs args)
        {
            UpdatePlayPauseIconAndText();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                if (null == PropertyChanged) return;

                if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
                }
            }
            catch { }
        }
    }
}
