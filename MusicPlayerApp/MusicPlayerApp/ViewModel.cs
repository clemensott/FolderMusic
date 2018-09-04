using LibraryLib;
using PlayerIcons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace MusicPlayerApp
{
    public class ViewModel : INotifyPropertyChanged
    {
        private bool mainPageLoaded = false, sliderEntered = false, scrollLbxCurrentPlaylistChanged = true;
        private int openPlaylistsIndex = 0;
        private double sliderValue = 0;
        private ListBox lbxCurrentPlaylist, lbxPlaylists;
        private SymbolIcon playIcon = new SymbolIcon(Symbol.Play), pausIcon = new SymbolIcon(Symbol.Pause);

        public bool MainPageLoaded { get { return mainPageLoaded; } }

        public bool IsOpenPlaylistCurrentPlaylist
        {
            get { return CurrentPlaylistIndex == openPlaylistsIndex; }
        }

        public int OpenPlaylistIndex { get { return openPlaylistsIndex; } }

        public int ShuffleListIndex
        {
            get { return CurrentPlaylist.ShuffleListIndex; }
            set
            {
                UiUpdate.ShuffleListIndex();

                if (scrollLbxCurrentPlaylistChanged)
                {
                    scrollLbxCurrentPlaylistChanged = false;
                    LbxCurrentPlaylistScollToSelectedItem();
                }
            }
        }

        public int CurrentPlaylistIndex
        {
            get { return Library.Current.CurrentPlaylistIndex; }
            set { UiUpdate.CurrentPlaylistIndex(); }
        }

        public double CurrentSongPositionMilliseconds
        {
            get
            {
                if (Library.Current.CurrentSong.IsEmptyOrLoading) return 0;
                if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Closed ||
                    BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped)
                {
                    return Library.Current.CurrentPlaylist.SongPositionMilliseconds;
                }

                return sliderEntered ? sliderValue : BackgroundMediaPlayer.Current.Position.TotalMilliseconds;
            }

            set
            {
                if (sliderValue == value) return;

                sliderValue = value;
                UiUpdate.CurrentSongPosition();
            }
        }

        public double CurrentSongNaturalDurationMilliseconds
        {
            get
            {
                if (Library.Current.CurrentSong.IsEmptyOrLoading) return 1;
                return BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds != 0 ?
                    BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds :
                    Library.Current.CurrentSong.NaturalDurationMilliseconds;
            }
        }

        public string CurrentPlaylistName { get { return CurrentPlaylist.Name; } }

        public string CurrentSongTitle { get { return Library.Current.CurrentSong.Title; } }

        public string CurrentSongArtist { get { return Library.Current.CurrentSong.Artist; } }

        public string CurrentSongPostionText { get { return GetShowTime(CurrentSongPositionMilliseconds); } }

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
                return BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing ? pausIcon : playIcon;
            }
        }

        public Visibility CurrentSongArtistVisibility { get { return Library.Current.CurrentSong.ArtistVisibility; } }

        public ImageSource LoopIcon { get { return CurrentPlaylist != null ? CurrentPlaylist.LoopIcon : Icons.LoopOff; } }

        public ImageSource ShuffleIcon { get { return CurrentPlaylist != null ? CurrentPlaylist.ShuffleIcon : Icons.ShuffleOff; } }

        public List<Song> CurrentPlaylistSongs { get { return CurrentPlaylist.GetShuffleSongs(); } }

        public Playlist CurrentPlaylist
        {
            get { return Library.Current.CurrentPlaylist; }
            set
            {
                if (Library.Current.IsEmpty) return;
                int index = GetIndexOfPlaylist(value);

                if (index != -1) Library.Current.CurrentPlaylistIndex = index;
                if (Library.Current.CurrentPlaylistIndex != index) return;

                UiUpdate.CurrentPlaylistIndexAndRest();
            }
        }

        public Playlist OpenPlaylist
        {
            get { return Library.Current[openPlaylistsIndex]; }
            set
            {
                if (Library.Current.IsEmpty) return;
                int index = GetIndexOfPlaylist(value);

                openPlaylistsIndex = index != -1 ? index : CurrentPlaylistIndex;
            }
        }

        public List<Playlist> Playlists { get { return Library.Current.GetPlaylists(); } }

        public ViewModel() { }

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

        public void SetLbxCurrentPlaylist(ListBox listBox)
        {
            lbxCurrentPlaylist = listBox;
        }

        public void LbxCurrentPlaylistScollToSelectedItem()
        {
            if (lbxCurrentPlaylist == null) return;

            lbxCurrentPlaylist.ScrollIntoView(CurrentPlaylist.CurrentSong);
        }

        public void SetLbxPlaylists(ListBox listBox)
        {
            lbxPlaylists = listBox;
        }

        public void LbxPlaylistsScollToSelectedItem()
        {
            if (lbxPlaylists == null || lbxPlaylists.SelectedIndex == -1) return;

            lbxPlaylists.ScrollIntoView(lbxPlaylists.SelectedItem);
        }

        public void SetMainPageLoaded()
        {
            openPlaylistsIndex = CurrentPlaylistIndex;
            mainPageLoaded = true;
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
            UiUpdate.CurrentSongPosition();
        }

        public void SetChangedCurrentPlaylistIndex()
        {
            scrollLbxCurrentPlaylistChanged = true;
        }

        private int GetIndexOfPlaylist(Playlist playlist)
        {
            int index = Library.Current.GetPlaylistIndex(playlist);

            return index < 0 && index >= Library.Current.Length ? -1 : index;
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
