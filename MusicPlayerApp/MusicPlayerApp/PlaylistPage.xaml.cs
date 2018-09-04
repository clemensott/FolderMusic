using FolderMusicLib;
using LibraryLib;
using System.Threading.Tasks;
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

        private ListBox lbxDefault, lbxShuffle;

        public static bool Open { get { return playlistPageOpen; } }

        public static PlaylistPage Current { get { return Open ? page : null; } }

        public PlaylistPage()
        {
            this.InitializeComponent();
            page = this;
            playlistPageOpen = true;

            playlist = ViewModel.Current.OpenPlaylist;
            DataContext = playlist;

            Library.Current.ScrollToIndex += Library_SrcollToIndex;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public static void GoBack()
        {
            playlistPageOpen = false;

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

            playlist.RemoveSong(playlist.Songs.IndexOf(song));

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
            lbxDefault = sender as ListBox;
            ScrollToCurrentSong(lbxDefault);
        }

        private void LbxShuffleSongs_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            lbxShuffle = sender as ListBox;
            ScrollToCurrentSong(lbxShuffle);
        }

        private void ScrollToCurrentSong(ListBox lbx)
        {
            if (lbx == null || !lbx.Items.Contains(playlist.CurrentSong)) return;

            lbx.ScrollIntoView(playlist.CurrentSong);
        }

        private async void Library_SrcollToIndex(object sender, Playlist e)
        {
            if (lbxDefault == null || lbxShuffle == null) return;

            while (lbxDefault.Items.Count < e.SongsIndex || lbxShuffle.Items.Count < e.ShuffleListIndex)
            {
                await Task.Delay(10);
            }

            lbxDefault.ScrollIntoView(lbxDefault.Items[e.SongsIndex]);
            lbxShuffle.ScrollIntoView(lbxShuffle.Items[e.ShuffleListIndex]);
        }

        private async void RefreshThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            await LoadingPage.NavigateTo();
            await playlist.LoadSongsFromStorage();
            LoadingPage.GoBack();

            if (playlist.IsEmptyOrLoading) GoBack();
        }

        private async void SearchForNewSongs_Click(object sender, RoutedEventArgs e)
        {
            await LoadingPage.NavigateTo();
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
