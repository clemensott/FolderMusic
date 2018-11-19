using MusicPlayer.Data;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace FolderMusic
{
    public sealed partial class PlaylistsView : UserControl
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ILibrary), typeof(PlaylistsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnSourcePropertyChanged)));

        private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (PlaylistsView)sender;
            var oldLibrary = (ILibrary)e.OldValue;
            var newLibrary = (ILibrary)e.NewValue;

            s.Unsubscribe(oldLibrary);
            s.Subscribe(newLibrary);
            s.SetItemsSourceSafe();
        }

        private bool isPointerOnDetailIcon;

        public ILibrary Source
        {
            get { return (ILibrary)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public PlaylistsView()
        {
            this.InitializeComponent();

            isPointerOnDetailIcon = false;
        }

        private void Subscribe(ILibrary library)
        {
            if (library == null) return;

            if (!library.IsLoaded) library.Loaded += OnSourceLoaded;
            else
            {
                library.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
                library.PlaylistsChanged += OnPlaylistsChanged;
            }
        }

        private void Unsubscribe(ILibrary library)
        {
            if (library == null) return;

            library.Loaded -= OnSourceLoaded;
            library.CurrentPlaylistChanged -= OnCurrentPlaylistChanged;
        }

        private void OnSourceLoaded(object sender, EventArgs args)
        {
            Unsubscribe(Source);
            Subscribe(Source);

            SetItemsSourceSafe();
        }

        private void OnCurrentPlaylistChanged(object sender, CurrentPlaylistChangedEventArgs e)
        {
            SetSelectedPlaylistSafe();
        }

        private void OnPlaylistsChanged(object sender, PlaylistsChangedEventArgs e)
        {
            SetItemsSourceSafe();
        }

        private void SetSelectedPlaylistSafe()
        {
            Utils.DoSafe(SetSelectedPlaylist);
        }

        private void SetSelectedPlaylist()
        {
            lbxPlaylists.SelectedItem = Source?.CurrentPlaylist;
        }

        private void SetItemsSourceSafe()
        {
            Utils.DoSafe(SetItemsSource);
        }

        private void SetItemsSource()
        {
            if (Source == null) return;

            lbxPlaylists.ItemsSource = new PlaylistsUpdateCollection(Source.Playlists);
            SetSelectedPlaylist();
        }

        private void ItemsSource_UpdateFinished(IUpdateSelectedItemCollection<IPlaylist> sender)
        {
            SetSelectedPlaylistSafe();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((Source?.Playlists?.Count ?? 0) == 0) return;

            IPlaylist selectedPlaylist = lbxPlaylists.SelectedItem as IPlaylist;

            if (!isPointerOnDetailIcon && selectedPlaylist != null) Source.CurrentPlaylist = selectedPlaylist;
            else if (lbxPlaylists.Items.Contains(Source.CurrentPlaylist))
            {
                lbxPlaylists.SelectedItem = Source.CurrentPlaylist;
            }
        }

        private void Playlist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void ResetPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (Source == null) return;

            GetFrame().Navigate(typeof(LoadingPage), Source);

            IPlaylist playlist = (sender as MenuFlyoutItem).DataContext as IPlaylist;
            await playlist.Reset();

            GetFrame().GoBack();
        }

        private async void UpdatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (Source == null) return;

            GetFrame().Navigate(typeof(LoadingPage), Source);

            IPlaylist playlist = (sender as MenuFlyoutItem).DataContext as IPlaylist;
            await playlist.Update();

            GetFrame().GoBack();
        }

        private async void ResetSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (Source == null) return;

            GetFrame().Navigate(typeof(LoadingPage), Source);

            IPlaylist playlist = (sender as MenuFlyoutItem).DataContext as IPlaylist;
            await playlist.ResetSongs();

            GetFrame().GoBack();
        }

        private async void SearchForNewSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (Source == null) return;

            GetFrame().Navigate(typeof(LoadingPage), Source);

            IPlaylist playlist = (sender as MenuFlyoutItem).DataContext as IPlaylist;
            await playlist.AddNew();

            GetFrame().GoBack();
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Source == null) return;

            IPlaylist playlist = (sender as Image).DataContext as IPlaylist;

            Source.CurrentPlaylist = playlist;
            Source.IsPlaying = true;
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
            IPlaylist playlist = (sender as MenuFlyoutItem).DataContext as IPlaylist;

            Source?.Playlists.Remove(playlist);
        }

        private void Playlist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Source != null) Source.CurrentPlaylist = (sender as Grid).DataContext as IPlaylist;
        }
    }
}
