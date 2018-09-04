using FolderMusicLib;
using LibraryLib;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace MusicPlayerApp
{
    public sealed partial class PlaylistPage : Page
    {
        private static bool playlistPageOpen;
        private static PlaylistPage page;

        private Playlist playlist;

        public static bool Open { get { return playlistPageOpen; } }

        public static PlaylistPage Current { get { return Open ? page : null; } }

        public PlaylistPage()
        {
            this.InitializeComponent();
            page = this;
            playlistPageOpen = true;

            playlist = ViewModel.Current.OpenPlaylist;
            DataContext = playlist;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public static void GoBack()
        {
            playlistPageOpen = false;

            page.playlist.SetDefaultSongsLbx(null);
            page.playlist.SetShuffleSongsLbx(null);

            page.Frame.GoBack();
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            playlist.SetNextShuffle();
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            playlist.SetNextLoop();
        }

        private async void RefreshSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            await song.Refresh();
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;
            int songsIndex = playlist.Songs.IndexOf(song);

            Library.Current.RemoveSongFromPlaylist(playlist, songsIndex);

            if (playlist.IsEmptyOrLoading) GoBack();
        }

        private void Song_Tapped(object sender, TappedRoutedEventArgs e)
        {
            GoBack();
        }

        private void CurrentPlaylistSong_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (((sender as Grid).DataContext as Song).IsEmptyOrLoading) return;
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void LbxDefaultSongs_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            playlist.SetDefaultSongsLbx(sender as ListBox);
        }

        private void LbxShuffleSongs_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            playlist.SetShuffleSongsLbx(sender as ListBox);
        }

        private async void RefreshThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            LoadingPage.OpenLoading(Frame);

            await playlist.LoadSongsFromStorage();

            LoadingPage.GoBack();

            if (playlist.IsEmptyOrLoading) GoBack();
        }

        private async void SearchForNewSongs_Click(object sender, RoutedEventArgs e)
        {
            LoadingPage.OpenLoading(Frame);

            await playlist.SearchForNewSongs();

            LoadingPage.GoBack();
        }

        private void DeleteThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Library.Current.Delete(playlist);
            GoBack();
        }
    }
}
