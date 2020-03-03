using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using MusicPlayer.Models.Interfaces;

// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace FolderMusic
{
    public sealed partial class PlaylistsView : UserControl
    {
        public static readonly DependencyProperty CurrentPlaylistProperty =
            DependencyProperty.Register("CurrentPlaylist", typeof(IPlaylist), typeof(PlaylistsView),
                new PropertyMetadata(null, OnCurrentPlaylistPropertyChanged));

        private static void OnCurrentPlaylistPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PlaylistsView s = (PlaylistsView)sender;

            s.SetSelectedPlaylist();
        }

        public static readonly DependencyProperty PlaylistsProperty =
            DependencyProperty.Register("Playlists", typeof(IEnumerable<IPlaylist>), typeof(PlaylistsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnPlaylistsPropertyChanged)));

        private static void OnPlaylistsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PlaylistsView s = (PlaylistsView)sender;
            IEnumerable<IPlaylist> value = (IEnumerable<IPlaylist>)e.NewValue;

            s.lbxPlaylists.ItemsSource = value;
            s.SetSelectedPlaylist();
        }

        private bool isPointerOnDetailIcon;

        public event EventHandler<PlaylistActionEventArgs> UpdateClick;
        public event EventHandler<PlaylistActionEventArgs> ResetClick;
        public event EventHandler<PlaylistActionEventArgs> ResetSongsClick;
        public event EventHandler<PlaylistActionEventArgs> AddNewClick;
        public event EventHandler<PlaylistActionEventArgs> RemoveClick;
        public event EventHandler<PlaylistActionEventArgs> PlayClick;
        public event EventHandler<PlaylistActionEventArgs> DetailsClick;

        public IPlaylist CurrentPlaylist
        {
            get { return (IPlaylist)GetValue(CurrentPlaylistProperty); }
            set { SetValue(CurrentPlaylistProperty, value); }
        }

        public IEnumerable<IPlaylist> Playlists
        {
            get { return (IEnumerable<IPlaylist>)GetValue(PlaylistsProperty); }
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

        private void ResetPlaylist_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;

            ResetClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void UpdatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;

            UpdateClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void ResetSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;

            ResetSongsClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void SearchForNewSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;

            AddNewClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;
            MobileDebug.Service.WriteEvent("ImgPlayTapped1", playlist?.AbsolutePath);

            PlayClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void DetailPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;
            MobileDebug.Service.WriteEvent("ImgDetailTapped1", playlist?.AbsolutePath);

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

        private Frame GetFrame()
        {
            return Window.Current.Content as Frame;
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)((FrameworkElement)sender).DataContext;

            RemoveClick?.Invoke(this, new PlaylistActionEventArgs(playlist));
        }

        private void Playlist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CurrentPlaylist = ((FrameworkElement)sender).DataContext as IPlaylist;
        }
    }
}
