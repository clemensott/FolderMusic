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

        private bool mainPageLoaded, sliderEntered = false, scrollLbxCurrentPlaylist = true;
        private int openPlaylistsIndex = 0;
        private double sliderMaximum;
        private SymbolIcon playIcon, pauseIcon;
        private ListBox lbxCurrentPlaylist;

        public static ViewModel Current
        {
            get
            {
                if (!Library.Current.IsForeground) return null;
                if (instance == null) instance = new ViewModel();

                return instance;
            }
        }

        public bool IsMainPageLoaded { get { return mainPageLoaded; } }

        public int PlaylistsIndex
        {
            get { return Library.Current.CurrentPlaylistIndex; }
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

        public double BackgroundPlayerPositionMilliseconds
        {
            get { return BackgroundMediaPlayer.Current.Position.TotalMilliseconds; }
        }

        public double BackgroundPlayerNaturalDurationMilliseconds
        {
            get { return BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds; }
        }

        public double SliderValue
        {
            get { return CurrentPlaylist.SongPositionMilliseconds; }
            set
            {
                if (Math.Abs(SliderValue - value) < 100) return;
                
                CurrentPlaylist.SongPositionMilliseconds = value;
                UpdateSliderValueText();
            }
        }

        public double SliderMaximum { get { return sliderMaximum; } }

        public string SliderValueText { get { return GetShowTime(SliderValue); } }

        public string SliderMaximumText
        {
            get { return GetShowTime(SliderMaximum); }
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

        public Playlist CurrentPlaylist { get { return Library.Current.CurrentPlaylist; } }

        public Playlist OpenPlaylist
        {
            get { return Library.Current[openPlaylistsIndex]; }
            set
            {
                if (Library.Current.IsEmpty) return;

                openPlaylistsIndex = value.PlaylistIndex;

                if (openPlaylistsIndex == -1) openPlaylistsIndex = Library.Current.CurrentPlaylistIndex;
            }
        }

        public List<Playlist> Playlists { get { return Library.Current.Playlists; } }

        private ViewModel()
        {
            try
            {
                Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, 
                    ()=> {
                        playIcon = new SymbolIcon(Symbol.Play);
                        pauseIcon = new SymbolIcon(Symbol.Pause);
                    });
            }
            catch { }
        }

        private void LoadPlayPauseSymbols()
        {
            playIcon = new SymbolIcon(Symbol.Play);
            pauseIcon = new SymbolIcon(Symbol.Pause);
        }

        private void ChangeSliderValue()
        {
            if (CurrentPlaylist.CurrentSong.IsEmptyOrLoading) CurrentPlaylist.SongPositionMilliseconds = 0;
            else if (BackgroundMediaPlayer.Current.Position.TotalMilliseconds == 0) return;
            else if (!sliderEntered) CurrentPlaylist.SongPositionMilliseconds = BackgroundPlayerPositionMilliseconds;
        }

        private void ChangeSliderMaximum()
        {
            if (CurrentPlaylist.CurrentSong.IsEmptyOrLoading) sliderMaximum = 2;
            else
            {
                sliderMaximum = BackgroundPlayerNaturalDurationMilliseconds != 0 ?
                    BackgroundPlayerNaturalDurationMilliseconds : CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds;
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
            mainPageLoaded = Library.Current.IsForeground;
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
            BackgroundMediaPlayer.Current.Position = TimeSpan.FromMilliseconds(SliderValue);
        }

        public void Play()
        {
            if (Library.Current.IsForeground) BackgroundCommunicator.SendPlay();
        }

        public void Pause()
        {
            if (Library.Current.IsForeground) BackgroundCommunicator.SendPause();
        }

        public void UpdateSliderMaximumAndSliderValue()
        {
            UpdateSliderMaximum();
            UpdateSliderValue();
        }

        public void UpdateSliderValue()
        {
            ChangeSliderValue();
            NotifyPropertyChanged("SliderValue");
            UpdateSliderValueText();

            if (SliderMaximum < 2) UpdateSliderMaximum();
        }

        private void UpdateSliderValueText()
        {
            NotifyPropertyChanged("SliderValueText");
        }

        public void UpdateSliderMaximum()
        {
            ChangeSliderMaximum();
            NotifyPropertyChanged("SliderMaximum");
            NotifyPropertyChanged("SliderMaximumText");
        }

        public void UpdatePlayPauseIconAndText()
        {
            NotifyPropertyChanged("PlayPauseIcon");
            NotifyPropertyChanged("PlayPauseText");
        }

        public void UpdatePlaylistsAndIndex()
        {
            NotifyPropertyChanged("Playlists");
            NotifyPropertyChanged("PlaylistsIndex");
        }

        public void UpdatePlaylistIndex()
        {
            NotifyPropertyChanged("PlaylistsIndex");
        }

        public void UpdateCurrentPlaylistIndexAndRest()
        {
            SetScrollLbxCurrentPlaylist();
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
