using LibraryLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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

        public static PlaylistPage Current { get { return Open ? page : null; } }

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
            viewModel.ScrollIntoLbxDefault();
            viewModel.ScrollIntoLbxShuffle();
        }

        public static void GoBack()
        {
            playlistPageOpen = false;
            page.Frame.GoBack();
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            App.ViewModel.SetChangedCurrentPlaylistIndex();
            viewModel.SetScrollLbxShuffle();

            BackgroundCommunicator.SendShuffle(App.ViewModel.OpenPlaylistIndex);
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            playlist.SetNextLoop();
            viewModel.UpdateLoopIcon();

            if (App.ViewModel.IsOpenPlaylistCurrentPlaylist)
            {
                UiUpdate.LoopIcon();
            }

            BackgroundCommunicator.SendLoop(App.ViewModel.OpenPlaylistIndex);
        }

        private async void RefreshSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            await song.Refresh();

            BackgroundCommunicator.SendSong(song);
            song.UpdateTitleAndArtist();
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            bool same = App.ViewModel.IsOpenPlaylistCurrentPlaylist;
            Song song = (sender as MenuFlyoutItem).DataContext as Song;
            int songsIndex = playlist.Songs.IndexOf(song);

            Library.Current.RemoveSongFromPlaylist(playlist, songsIndex);
            BackgroundCommunicator.SendRemoveSong(App.ViewModel.OpenPlaylistIndex, songsIndex);

            if (playlist.IsEmptyOrLoading)
            {
                UiUpdate.Playlists();
                if (same) UiUpdate.CurrentPlaylistIndexAndRest();

                GoBack();
                return;
            }

            viewModel.UpdateDefaultSongs();
            viewModel.UpdateShuffleSongsAndIcon();

            playlist.UpdateSongCount();
            if (same) UiUpdate.CurrentPlaylistSongs();
        }

        private void DefaultSong_Tapped(object sender, TappedRoutedEventArgs e)
        {
            bool same = App.ViewModel.IsOpenPlaylistCurrentPlaylist;
            int songsIndex = playlist.Songs.IndexOf((sender as Grid).DataContext as Song);

            BackgroundCommunicator.SendPlaylistPageTap(songsIndex);
            DoSongTappedSame(same);
        }

        private void ShuffleSong_Tapped(object sender, TappedRoutedEventArgs e)
        {
            bool same = App.ViewModel.IsOpenPlaylistCurrentPlaylist;
            playlist.SongsIndex = playlist.Songs.IndexOf((sender as Grid).DataContext as Song);

            BackgroundCommunicator.SendPlaylistPageTap(playlist.SongsIndex);
            DoSongTappedSame(same);
        }

        private void DoSongTappedSame(bool same)
        {
            App.ViewModel.CurrentPlaylist = playlist;

            if (same) UiUpdate.CurrentPlaylistIndexAndRest();

            GoBack();
        }

        private void CurrentPlaylistSong_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (((sender as Grid).DataContext as Song).IsEmptyOrLoading) return;
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        public void UpdateUi()
        {
            if (playlist.IsEmptyOrLoading)
            {
                GoBack();
                return;
            }

            viewModel.UpdateDefaultSongs();
            viewModel.UpdateShuffleSongsAndIcon();
        }

        private void LbxDefaultSongs_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            viewModel.SetLbxDefault(sender as ListBox);
        }

        private void LbxShuffleSongs_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            viewModel.SetLbxShuffle(sender as ListBox);
        }
    }

    class PlaylistPageViewModel : INotifyPropertyChanged
    {
        private bool scrollLbxShuffle = true;
        private Playlist playlist;

        private ListBox lbxDefault,lbxShuffle;

        public int DefaultCurrentSongIndex
        {
            get { return playlist.SongsIndex; }
            set { NotifyPropertyChanged("DefaultCurrentSongIndex"); }
        }

        public int ShuffleCurrentSongIndex
        {
            get { return playlist.ShuffleListIndex; }
            set
            {
                NotifyPropertyChanged("ShuffleCurrentSongIndex");

                if (scrollLbxShuffle)
                {
                    scrollLbxShuffle = false;
                    ScrollIntoLbxShuffle();
                }
            }
        }

        public string Name { get { return playlist.Name; } }

        public string RelativePath { get { return playlist.RelativePath; } }

        public ImageSource ShuffleIcon { get { return playlist.ShuffleIcon; } }

        public ImageSource LoopIcon { get { return playlist.LoopIcon; } }

        public List<Song> DefaultSongs { get { return playlist.Songs; } }

        public List<Song> ShuffleSongs { get { return playlist.GetShuffleSongs(); } }

        public PlaylistPageViewModel(Playlist playlist)
        {
            this.playlist = playlist;
        }

        public void SetLbxDefault(ListBox listBox)
        {
            lbxDefault = listBox;
        }

        public void ScrollIntoLbxDefault()
        {
            if (lbxDefault == null) return;

            lbxDefault.ScrollIntoView(playlist.CurrentSong);
        }

        public void ScrollIntoLbxShuffle()
        {
            if (lbxShuffle == null) return;

            lbxShuffle.ScrollIntoView(playlist.CurrentSong);
        }

        public void SetLbxShuffle(ListBox listBox)
        {
            lbxShuffle = listBox;
        }

        public void SetScrollLbxShuffle()
        {
            scrollLbxShuffle = true;
            App.ViewModel.SetChangedCurrentPlaylistIndex();
        }

        public void UpdateDefaultSongs()
        {
            NotifyPropertyChanged("DefaultSongs");
        }

        public void UpdateShuffleSongsAndIcon()
        {
            NotifyPropertyChanged("ShuffleSongs");
            UpdateShuffleIcon();
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
