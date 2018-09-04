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
        private bool mainPageLoaded = false, lbxCurrentPlaylistEntered = false, sliderEntered = false;
        private int lbxPlaylistsChangedIndex;
        private double sliderValue = 0;

        public bool MainPageLoaded { get { return mainPageLoaded; } }

        public bool IsOpenPlaylistCurrentPlaylist
        {
            get { return CurrentPlaylistIndex == lbxPlaylistsChangedIndex; }
        }

        public int OpenPlaylistIndex { get { return lbxPlaylistsChangedIndex; } }

        public int CurrentSongIndex
        {
            get { return CurrentPlaylist.CurrentSongIndex; }
            set
            {
                if (lbxCurrentPlaylistEntered && CurrentSongIndex != value)
                {
                    CurrentPlaylist.CurrentSongIndex = value;
                    BackgroundCommunicator.SendPlaySong(CurrentSongIndex);
                    UpdateCurrentSongTitleArtistNaturalDuration();
                }
            }
        }

        public int CurrentPlaylistIndex
        {
            get { return Library.Current.CurrentPlaylistIndex; }
            set
            {
                lbxPlaylistsChangedIndex = value;

                if (Library.Current.CurrentPlaylistIndex != value)
                {
                    NotifyPropertyChanged("CurrentPlaylistIndex");
                }
            }
        }

        public double CurrentSongPositionMilliseconds
        {
            get { return sliderEntered ? sliderValue : BackgroundMediaPlayer.Current.Position.TotalMilliseconds; }
            set
            {
                if (sliderValue != value)
                {
                    sliderValue = value;
                    UpdateCurrentSongPosition();
                }
            }
        }

        public double CurrentSongNaturalDurationMilliseconds
        {
            get
            {
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

        public Playlist CurrentPlaylist { get { return Library.Current.CurrentPlaylist; } }

        public Playlist OpenPlaylist
        {
            get
            {
                return Library.Current[lbxPlaylistsChangedIndex];
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
            lbxPlaylistsChangedIndex = CurrentPlaylistIndex;
            mainPageLoaded = true;
        }

        public void LbxCurrentPlaylistEntered()
        {
            lbxCurrentPlaylistEntered = true;
        }

        public void LbxCurrentPlaylistExited()
        {
            lbxCurrentPlaylistEntered = false;
        }

        public void SetLbxPlaylistsChangedIndex(int index)
        {
            lbxPlaylistsChangedIndex = index;
        }

        public void SetCurrentPlaylistIndex()
        {
            if (CurrentPlaylistIndex != lbxPlaylistsChangedIndex)
            {
                Library.Current.CurrentPlaylistIndex = lbxPlaylistsChangedIndex;
                UpdateCurrentPlaylistsIndexAndRest();
            }
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
            UpdateCurrentSongPosition();
        }

        public void UpdateCurrentSongTitleArtistNaturalDuration()
        {
            NotifyPropertyChanged("CurrentSongIndex");
            NotifyPropertyChanged("CurrentSongTitle");
            NotifyPropertyChanged("CurrentSongArtist");

            UpdateCurrentSongNaturalDuration();
            UpdateCurrentSongPosition();
        }

        public void UpdateCurrentSongNaturalDuration()
        {
            if (CurrentSongNaturalDurationMilliseconds < 2) return;

            CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds = CurrentSongNaturalDurationMilliseconds;

            NotifyPropertyChanged("CurrentSongNaturalDurationText");
            NotifyPropertyChanged("CurrentSongNaturalDurationMilliseconds");
        }

        public void UpdateCurrentSongPosition()
        {
            NotifyPropertyChanged("CurrentSongPostionText");
            NotifyPropertyChanged("CurrentSongPositionMilliseconds");
        }

        public void UpdatePlayPauseIcon()
        {
            NotifyPropertyChanged("PlayPauseIcon");
        }

        public void UpdateShuffleIcon()
        {
            NotifyPropertyChanged("ShuffleIcon");
        }

        public void UpdateLoopIcon()
        {
            NotifyPropertyChanged("LoopIcon");
        }

        public void UpdatePlaylistsAndCurrentPlaylistIndex()
        {
            NotifyPropertyChanged("Playlists");
            NotifyPropertyChanged("CurrentPlaylistIndex");
        }

        public void UpdateCurrentPlaylistsIndexAndRest()
        {
            UpdateCurrentPlaylistSongsAndIndex();
            NotifyPropertyChanged("CurrentPlaylistName");
            UpdateLoopIcon();
            UpdateShuffleIcon();
            UpdateCurrentSongTitleArtistNaturalDuration();
        }

        public void UpdateCurrentPlaylistSongsAndIndex()
        {
            NotifyPropertyChanged("CurrentPlaylistSongs");
            NotifyPropertyChanged("CurrentPlaylistIndex");
        }

        public void UpdateCurrentSongIndex()
        {
            NotifyPropertyChanged("CurrentSongIndex");
        }

        public void UpdateAfterActivating()
        {
            UpdateCurrentSongTitleArtistNaturalDuration();
            UpdatePlayPauseIcon();
        }

        public void UpdateAll()
        {
            NotifyPropertyChanged("CurrentPlaylistSongs");
            NotifyPropertyChanged("Playlists");

            NotifyPropertyChanged("CurrentSongIndex");
            NotifyPropertyChanged("CurrentPlaylistIndex");
            NotifyPropertyChanged("CurrentPlaylistName");

            NotifyPropertyChanged("CurrentSongTitle");
            NotifyPropertyChanged("CurrentSongArtist");

            NotifyPropertyChanged("CurrentSongPostionText");
            NotifyPropertyChanged("CurrentSongPositionMilliseconds");
            NotifyPropertyChanged("CurrentSongNaturalDurationText");
            NotifyPropertyChanged("CurrentSongNaturalDurationMilliseconds");

            NotifyPropertyChanged("LoopIcon");
            NotifyPropertyChanged("NextIcon");
            NotifyPropertyChanged("PlayPauseIcon");
            NotifyPropertyChanged("PreviousIcon");
            NotifyPropertyChanged("ShuffleIcon");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                if (null != PropertyChanged)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.
                        CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    );
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(propertyName);
            }
        }
    }
}
