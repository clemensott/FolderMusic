using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using MusicPlayer.Models.Foreground.Interfaces;
using MusicPlayer.Models.EventArgs;
using System.Linq;

// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace FolderMusic
{
    public sealed partial class PlaylistsView : UserControl
    {
        public static readonly DependencyProperty CurrentPlaylistProperty =
            DependencyProperty.Register(nameof(CurrentPlaylist), typeof(IPlaylist), typeof(PlaylistsView),
                new PropertyMetadata(null, OnCurrentPlaylistPropertyChanged));

        private static void OnCurrentPlaylistPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PlaylistsView s = (PlaylistsView)sender;

            s.SetSelectedPlaylist();
        }

        public static readonly DependencyProperty PlaylistsProperty =
            DependencyProperty.Register(nameof(Playlists), typeof(IPlaylistCollection), typeof(PlaylistsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnPlaylistsPropertyChanged)));

        private static void OnPlaylistsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PlaylistsView s = (PlaylistsView)sender;
            IPlaylistCollection oldValue = (IPlaylistCollection)e.OldValue;
            IPlaylistCollection newValue = (IPlaylistCollection)e.NewValue;

            s.lbxPlaylists.ItemsSource = newValue;
            s.SetSelectedPlaylist();

            if (oldValue != null) oldValue.Changed -= s.Playlists_Changed;
            if (newValue != null) newValue.Changed += s.Playlists_Changed;
        }

        private bool isPointerOnDetailIcon;

        public event EventHandler<PlaylistActionEventArgs> UpdateSongsClick;
        public event EventHandler<PlaylistActionEventArgs> UpdateFilesClick;
        public event EventHandler<PlaylistActionEventArgs> PlayClick;
        public event EventHandler<PlaylistActionEventArgs> DetailsClick;

        public IPlaylist CurrentPlaylist
        {
            get { return (IPlaylist)GetValue(CurrentPlaylistProperty); }
            set { SetValue(CurrentPlaylistProperty, value); }
        }

        public IPlaylistCollection Playlists
        {
            get { return (IPlaylistCollection)GetValue(PlaylistsProperty); }
            set { SetValue(PlaylistsProperty, value); }
        }

        public PlaylistsView()
        {
            this.InitializeComponent();

            isPointerOnDetailIcon = false;
        }

        private void Playlists_Changed(object sender, PlaylistCollectionChangedEventArgs e)
        {
            lbxPlaylists.ItemsSource = Playlists.ToArray();
        }

        private void SetSelectedPlaylist()
        {
            lbxPlaylists.SelectedItem = CurrentPlaylist;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IPlaylist selectedPlaylist = lbxPlaylists.SelectedItem as IPlaylist;

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

        private void MfiUpdateSongs_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;

            UpdateSongsClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void MfiUpdateFiles_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;

            UpdateFilesClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;

            PlayClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void DetailPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;

            DetailsClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void DetailPlaylist_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            isPointerOnDetailIcon = true;
        }

        private void DetailPlaylist_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            isPointerOnDetailIcon = false;
        }

        private void Playlist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CurrentPlaylist = ((FrameworkElement)sender).DataContext as IPlaylist;
        }
    }
}
