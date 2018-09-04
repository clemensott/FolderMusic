using MusicPlayerLib;
using PlayerIcons;
using System;
using System.ComponentModel;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace LibraryLib
{
    public class ViewModel : Library, INotifyPropertyChanged
    {
        private bool mainPageLoaded, sliderEntered = false, scrollLbxCurrentPlaylist = true;
        private int openPlaylistsIndex = 0;
        private double sliderValue = 0;
        private SymbolIcon playIcon = new SymbolIcon(Symbol.Play), pauseIcon = new SymbolIcon(Symbol.Pause);
        private ListBox lbxCurrentPlaylist;

        public bool IsMainPageLoaded { get { return mainPageLoaded; } }

        public int PlaylistsIndex
        {
            get { return CurrentPlaylistIndex; }
            set
            {
                openPlaylistsIndex = value;
                UpdatePlaylistIndex();
            }
        }

        public int OpenPlaylistIndex
        {
            get { return openPlaylistsIndex; }
            set { openPlaylistsIndex = value; }
        }

        public double CurrentSongPositionMilliseconds
        {
            get
            {
                if (CurrentPlaylist.CurrentSong.IsEmptyOrLoading) return 0;
                if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Closed ||
                    BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped)
                {
                    return CurrentPlaylist.SongPositionMilliseconds;
                }

                return sliderEntered ? sliderValue : BackgroundMediaPlayer.Current.Position.TotalMilliseconds;
            }

            set
            {
                if (sliderValue == value) return;

                sliderValue = value;
            }
        }

        public double CurrentSongNaturalDurationMilliseconds
        {
            get
            {
                if (CurrentPlaylist.CurrentSong.IsEmptyOrLoading) return 1;
                return BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds != 0 ?
                    BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds :
                    CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds;
            }
        }

        public string CurrentPlaylistName { get { return CurrentPlaylist.Name; } }

        public string CurrentSongTitle { get { return CurrentPlaylist.CurrentSong.Title; } }

        public string CurrentSongArtist { get { return CurrentPlaylist.CurrentSong.Artist; } }

        public string CurrentSongPositionText { get { return GetShowTime(CurrentSongPositionMilliseconds); } }

        public string CurrentSongNaturalDurationText
        {
            get { return GetShowTime(CurrentSongNaturalDurationMilliseconds); }
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
                return BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing ? pauseIcon : playIcon;
            }
        }

        public ImageSource LoopIcon { get { return CurrentPlaylist != null ? CurrentPlaylist.LoopIcon : Icons.LoopOff; } }

        public ImageSource ShuffleIcon { get { return CurrentPlaylist != null ? CurrentPlaylist.ShuffleIcon : Icons.ShuffleOff; } }

        public Playlist OpenPlaylist
        {
            get { return this[openPlaylistsIndex]; }
            set
            {
                if (IsEmpty) return;

                openPlaylistsIndex = value.PlaylistIndex;

                if (openPlaylistsIndex == -1) openPlaylistsIndex = CurrentPlaylistIndex;
            }
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
            mainPageLoaded = IsForeground;
        }

        public void SetLbxCurrentPlaylist(ListBox lbx)
        {
           lbxCurrentPlaylist = lbx;
            scrollLbxCurrentPlaylist = true;
        }

        public void SetScrollLbxCurrentPlaylist()
        {
            scrollLbxCurrentPlaylist = true;
        }

        public void DoScrollLbxCurrentPlaylist()
        {
            if (!scrollLbxCurrentPlaylist) return;

            scrollLbxCurrentPlaylist = false;

            if (lbxCurrentPlaylist == null || !lbxCurrentPlaylist.Items.Contains(CurrentPlaylist.CurrentSong)) return;

            lbxCurrentPlaylist.ScrollIntoView(CurrentPlaylist.CurrentSong);
        }

        public void EnteredSlider()
        {
            sliderEntered = true;
        }

        public void SetSliderValue()
        {
            if (!sliderEntered) return;

            sliderEntered = false;
            BackgroundMediaPlayer.Current.Position = TimeSpan.FromMilliseconds(sliderValue);

            NotifyPropertyChanged("CurrentSongPositionMilliseconds");
            NotifyPropertyChanged("CurrentSongPositionText");
        }

        public void Play()
        {
            if (IsForeground) BackgroundCommunicator.SendPlay();
        }

        public void Pause()
        {
            if (IsForeground) BackgroundCommunicator.SendPause();
        }

        public void UpdateCurrentSong()
        {
            NotifyPropertyChanged("CurrentSongTitle");
            NotifyPropertyChanged("CurrentSongArtist");

            NotifyPropertyChanged("CurrentSongNaturalDurationMilliseconds");
            NotifyPropertyChanged("CurrentSongNaturalDurationText");
            UpdateCurrentSongNaturalDuration();
        }

        public void UpdateCurrentSongNaturalDuration()
        {
            NotifyPropertyChanged("CurrentSongPositionMilliseconds");
            NotifyPropertyChanged("CurrentSongPositionText");
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
            SetScrollLbxCurrentPlaylist();
            UpdatePlaylistIndex();
            NotifyPropertyChanged("CurrentPlaylistName");

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
