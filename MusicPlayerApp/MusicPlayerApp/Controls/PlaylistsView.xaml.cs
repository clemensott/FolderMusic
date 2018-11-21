using FolderMusic.ViewModels;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace FolderMusic
{
    public sealed partial class PlaylistsView : UserControl
    {
        public static readonly DependencyProperty CurrentPlaylistProperty =
            DependencyProperty.Register("CurrentPlaylist", typeof(PlaylistViewModel), typeof(PlaylistsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnCurrentPlaylistPropertyChanged)));

        private static void OnCurrentPlaylistPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (PlaylistsView)sender;
            var value = (PlaylistViewModel)e.NewValue;

            s.SetSelectedPlaylist();
        }

        public static readonly DependencyProperty PlaylistsProperty =
            DependencyProperty.Register("Playlists", typeof(IEnumerable<PlaylistViewModel>), typeof(PlaylistsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnPlaylistsPropertyChanged)));

        private static void OnPlaylistsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (PlaylistsView)sender;
            var value = (IEnumerable<PlaylistViewModel>)e.NewValue;

            s.lbxPlaylists.ItemsSource = value;
            s.SetSelectedPlaylist();
        }

        private bool isPointerOnDetailIcon;

        public PlaylistViewModel CurrentPlaylist
        {
            get { return (PlaylistViewModel)GetValue(CurrentPlaylistProperty); }
            set { SetValue(CurrentPlaylistProperty, value); }
        }

        public IEnumerable<PlaylistViewModel> Playlists
        {
            get { return (IEnumerable<PlaylistViewModel>)GetValue(PlaylistsProperty); }
            set { SetValue(PlaylistsProperty, value); }
        }

        public PlaylistsView()
        {
            this.InitializeComponent();

            isPointerOnDetailIcon = false;
        }

        private void SetSelectedPlaylistSafe()
        {
            Utils.DoSafe(SetSelectedPlaylist);
        }

        private void SetSelectedPlaylist()
        {
            lbxPlaylists.SelectedItem = CurrentPlaylist;
        }

        private void ItemsSource_UpdateFinished(IUpdateSelectedItemCollection<PlaylistViewModel> sender)
        {
            SetSelectedPlaylistSafe();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlaylistViewModel selectedPlaylist = lbxPlaylists.SelectedItem as PlaylistViewModel;

            if (!isPointerOnDetailIcon && selectedPlaylist != null) CurrentPlaylist = selectedPlaylist;
            else if (lbxPlaylists.Items.Contains(CurrentPlaylist))
            {
                lbxPlaylists.SelectedItem = CurrentPlaylist;
            }
        }

        private void Playlist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void ResetPlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistViewModel playlist = (sender as MenuFlyoutItem).DataContext as PlaylistViewModel;

            GetFrame().Navigate(typeof(LoadingPage), playlist.Base.Parent.Parent);
            await playlist.Base.Reset();
            GetFrame().GoBack();
        }

        private async void UpdatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistViewModel playlist = (sender as MenuFlyoutItem).DataContext as PlaylistViewModel;

            GetFrame().Navigate(typeof(LoadingPage), playlist.Base.Parent.Parent);
            await playlist.Base.Update();
            GetFrame().GoBack();
        }

        private async void ResetSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistViewModel playlist = (sender as MenuFlyoutItem).DataContext as PlaylistViewModel;

            GetFrame().Navigate(typeof(LoadingPage), playlist.Base.Parent.Parent);
            await playlist.Base.ResetSongs();
            GetFrame().GoBack();
        }

        private async void SearchForNewSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistViewModel playlist = (sender as MenuFlyoutItem).DataContext as PlaylistViewModel;

            GetFrame().Navigate(typeof(LoadingPage), playlist.Base.Parent.Parent);
            await playlist.Base.AddNew();
            GetFrame().GoBack();
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PlaylistViewModel playlist = (sender as Image).DataContext as PlaylistViewModel;

            CurrentPlaylist = playlist;
            CurrentPlaylist.Base.Parent.Parent.CurrentPlaylist = CurrentPlaylist.Base;
        }

        private void DetailPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!PlaylistPage.Open) GetFrame().Navigate(typeof(PlaylistPage), (sender as Image).DataContext);
        }

        private void DetailPlaylist_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            isPointerOnDetailIcon = true;
        }

        private void DetailPlaylist_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            isPointerOnDetailIcon = false;
        }

        private Frame GetFrame()
        {
            return Window.Current.Content as Frame;
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistViewModel playlist = (sender as MenuFlyoutItem).DataContext as PlaylistViewModel;

            playlist.Base.Parent.Remove(playlist.Base);
        }

        private void Playlist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CurrentPlaylist = (sender as Grid).DataContext as PlaylistViewModel;
        }
    }
}
