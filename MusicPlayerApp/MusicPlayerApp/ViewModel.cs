using PlayerIcons;
using PlaylistSong;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace MusicPlayerApp
{
    public class ViewModel : INotifyPropertyChanged
    {
        private bool mainPageLoaded = false, sliderEntered = false, isUiEnabled = true;
        private int openPlaylistsIndex = 0;
        private double sliderValue = 0;

        public bool MainPageLoaded { get { return mainPageLoaded; } }

        public bool IsOpenPlaylistCurrentPlaylist
        {
            get { return CurrentPlaylistIndex == openPlaylistsIndex; }
        }

        public bool IsUiEnabled
        {
            get { return isUiEnabled; }
            set
            {
                if (isUiEnabled == value) return;

                isUiEnabled = value;
                NotifyPropertyChanged("IsUiEnabled");
            }
        }

        public int OpenPlaylistIndex { get { return openPlaylistsIndex; } }

        public int CurrentSongIndex
        {
            get { return CurrentPlaylist.CurrentSongIndex; }
            set { UiUpdate.CurrentSongIndex(); }
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
                if (CurrentPlaylist.CurrentSong.IsEmpty) return 0;
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
                if (CurrentPlaylist.CurrentSong.IsEmpty) return 1;
                return BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds != 0 ?
                    BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds :
                    CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds;
            }
        }

        public string CurrentPlaylistName { get { return CurrentPlaylist.Name; } }

        public string CurrentSongTitle { get { return CurrentPlaylist.CurrentSong.Title; } }

        public string CurrentSongArtist { get { return CurrentPlaylist.CurrentSong.Artist; } }

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
                IconElement icon = BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing ?
                    new SymbolIcon(Symbol.Pause) : new SymbolIcon(Symbol.Play);

                return icon;
            }
        }

        public Visibility CurrentSongArtistVisibility { get { return CurrentPlaylist.CurrentSong.ArtistVisibility; } }

        public ImageSource LoopIcon { get { return CurrentPlaylist != null ? CurrentPlaylist.LoopIcon : Icons.LoopOff; } }

        public ImageSource ShuffleIcon { get { return CurrentPlaylist != null ? CurrentPlaylist.ShuffleIcon : Icons.ShuffleOff; } }

        public List<Song> CurrentPlaylistSongs { get { return CurrentPlaylist.GetShuffleSongs(); } }

        public Playlist CurrentPlaylist
        {
            get { return Library.Current.CurrentPlaylist; }
            set
            {
                if (Library.IsEmpty()) return;
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
                if (Library.IsEmpty()) return;
                int index = GetIndexOfPlaylist(value);

                openPlaylistsIndex = index != -1 ? index : CurrentPlaylistIndex;
                //NotifyPropertyChanged("CurrentPlaylistIndex");
            }
        }

        public List<Playlist> Playlists { get { return Library.Current.GetPlaylists(); } }

        public ViewModel()
        {
            Library.Load();
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

        private int GetIndexOfPlaylist(Playlist playlist)
        {
            int index = Library.Current.GetPlaylists().IndexOf(playlist);

            return index < 0 && index >= Library.Current.Lenght ? -1 : index;
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
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(propertyName);
            }
        }
    }
}
