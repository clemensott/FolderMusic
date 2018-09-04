using LibraryLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace FolderMusicUwpLib
{
    public class ViewModel : INotifyPropertyChanged
    {
        private static ViewModel instance;

        private bool mainPageLoaded, sliderEntered = false;
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

        public bool IsMainPageLoaded { get { return mainPageLoaded; } }

        public int PlaylistsIndex
        {
            get { return Library.Current.CurrentPlaylistIndex; }
            set { UpdatePlaylistIndex(); }
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
                if (Math.Abs(SliderValue - value) < 100 || value >= CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds) return;
                
                CurrentPlaylist.SongPositionMilliseconds = value;

                if (sliderEntered) UpdateSliderValueText();
                else UpdateSliderValue();
            }
        }

        public double SliderMaximum
        {
            get { return Library.Current.CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds; }
            set
            {
                if (SliderMaximum == value) return;

                CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds = value;
                UpdateSliderMaximum();
            }
        }

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

        public void ChangeSliderMaximumAndSliderValue()
        {
            ChangeSliderMaximum();
            ChangeSliderValue();
        }

        public void ChangeSliderValue()
        {
            if (CurrentPlaylist.CurrentSong.IsEmptyOrLoading|| BackgroundPlayerPositionMilliseconds == 0) return;
            else if (!sliderEntered) SliderValue = BackgroundPlayerPositionMilliseconds;
        }

        public void ChangeSliderMaximum()
        {
            if (CurrentPlaylist.CurrentSong.IsEmptyOrLoading || BackgroundPlayerNaturalDurationMilliseconds == 0) return;
            else if (BackgroundPlayerNaturalDurationMilliseconds != 0)
            {
                SliderMaximum = BackgroundPlayerNaturalDurationMilliseconds;
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

        private void UpdateSliderValue()
        {
            if (Library.Current.CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds ==
                BackgroundPlayerNaturalDurationMilliseconds || SliderMaximum < 2)
            {
                UpdateSliderMaximum();
            }

            NotifyPropertyChanged("SliderValue");
            UpdateSliderValueText();
        }

        private void UpdateSliderValueText()
        {
            NotifyPropertyChanged("SliderValueText");
        }

        private void UpdateSliderMaximum()
        {
            NotifyPropertyChanged("SliderMaximum");
            NotifyPropertyChanged("SliderMaximumText");
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
