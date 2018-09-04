using PlaylistSong;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace MusicPlayerApp
{
    public sealed partial class PlaylistPage : Page
    {
        private static bool playlistPageOpen;
        private static PlaylistPage page;

        private Playlist playlist;
        private PlaylistPageViewModel viewModel;

        public static bool Open { get { return playlistPageOpen; } }

        public PlaylistPage()
        {
            this.InitializeComponent();
            page = this;
            playlistPageOpen = true;

            playlist = App.ViewModel.OpenPlaylist;
            viewModel = new PlaylistPageViewModel(playlist);
            DataContext = viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.LoadDefault();
            viewModel.LoadShuffle();
        }

        public static void GoBack()
        {
            playlistPageOpen = false;
            page.Frame.GoBack();
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            playlist.SetNextShuffle();
            viewModel.UpdateShuffleIcon();

            if (App.ViewModel.IsOpenPlaylistCurrentPlaylist)
            {
                App.ViewModel.UpdateShuffleIcon();
                App.ViewModel.UpdateCurrentPlaylistSongsAndIndex();
            }

            viewModel.LoadShuffle();
            BackgroundCommunicator.SendShuffle(App.ViewModel.OpenPlaylistIndex);
            Library.SaveAsync();
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            playlist.SetNextLoop();
            viewModel.UpdateLoopIcon();

            if (App.ViewModel.IsOpenPlaylistCurrentPlaylist)
            {
                App.ViewModel.UpdateLoopIcon();
            }

            BackgroundCommunicator.SendLoop(App.ViewModel.OpenPlaylistIndex);
            Library.SaveAsync();
        }

        private void lbxDefault_Tapped(object sender, TappedRoutedEventArgs e)
        {
            bool same = App.ViewModel.IsOpenPlaylistCurrentPlaylist;

            playlist.Shuffle = ShuffleKind.Off;
            viewModel.SetLbxDefaultLastSelectedIndex();

            App.ViewModel.SetCurrentPlaylistIndex();
            BackgroundCommunicator.SendPlaylistPageTap();

            if (same)
            {
                App.ViewModel.UpdateCurrentPlaylistsIndexAndRest();
            }

            GoBack();
        }

        private void lbxShuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            bool same = App.ViewModel.IsOpenPlaylistCurrentPlaylist;

            viewModel.SetLbxShuffleLastSelectedIndex();

            App.ViewModel.SetCurrentPlaylistIndex();
            BackgroundCommunicator.SendPlaylistPageTap();

            if (same)
            {
                App.ViewModel.UpdateCurrentSongTitleArtistNaturalDuration();
            }

            GoBack();
        }
    }

    class PlaylistPageViewModel : INotifyPropertyChanged
    {
        private int lbxDefaultLastSelectedIndex, lbxShuffleLastSelectedIndex;
        private Playlist playlist;

        public int DefaultCurrentSongIndex
        {
            get { return playlist.ShuffleList[playlist.CurrentSongIndex]; }
            set
            {
                lbxDefaultLastSelectedIndex = value;
            }
        }

        public int ShuffleCurrentSongIndex
        {
            get { return playlist.CurrentSongIndex; }
            set
            {
                lbxShuffleLastSelectedIndex = value;
            }
        }

        public string Name { get { return playlist.Name; } }

        public string RelativePath { get { return playlist.RelativePath; } }

        public ImageSource ShuffleIcon { get { return playlist.ShuffleIcon; } }

        public ImageSource LoopIcon { get { return playlist.LoopIcon; } }

        public List<Song> DefaultSongs { get { return playlist.GetSongs(); } }

        public List<Song> ShuffleSongs { get { return playlist.GetShuffleSongs(); } }

        public PlaylistPageViewModel(Playlist playlist)
        {
            this.playlist = playlist;
        }

        private List<Song> GetLoadSonglist()
        {
            List<Song> list = new List<Song>();
            list.Add(new Song());

            return list;
        }

        public void SetLbxDefaultLastSelectedIndex()
        {
            playlist.CurrentSongIndex = lbxDefaultLastSelectedIndex;
        }

        public void SetLbxShuffleLastSelectedIndex()
        {
            playlist.CurrentSongIndex = lbxShuffleLastSelectedIndex;
        }

        public void LoadDefault()
        {
            NotifyPropertyChanged("DefaultSongs");
        }

        public void LoadShuffle()
        {
            NotifyPropertyChanged("ShuffleSongs");
            NotifyPropertyChanged("ShuffleCurrentSongIndex");
        }

        public void UpdateShuffleIcon()
        {
            NotifyPropertyChanged("ShuffleIcon");
        }

        public void UpdateLoopIcon()
        {
            NotifyPropertyChanged("LoopIcon");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void NotifyPropertyChanged(String propertyName)
        {
            try
            {
                if (null != PropertyChanged)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    );
                }
            }
            catch { }
        }
    }
}
