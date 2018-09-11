using MusicPlayer.Data;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace FolderMusic
{
    public sealed partial class PlaylistPage : Page
    {
        private static bool playlistPageOpen;

        public static bool Open { get { return playlistPageOpen; } }

        private PlaylistViewModel viewModel;

        public IPlaylist Playlist { get; private set; }

        public PlaylistPage()
        {
            this.InitializeComponent();
            playlistPageOpen = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Playlist = e.Parameter as IPlaylist;
            DataContext = viewModel = new PlaylistViewModel(Playlist);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            playlistPageOpen = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Playlist.SetNextShuffle();
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Playlist.SetNextLoop();
        }

        private async void RefreshThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), Playlist.Parent.Parent);
            await Playlist.Refresh();
            Frame.GoBack();

            if (Playlist.SongsCount == 0) Frame.GoBack();
        }

        private async void SearchForNewSongs_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), Playlist.Parent.Parent);
            await Playlist.AddNew();
            Frame.GoBack();
        }

        private async void UpdateThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), Playlist.Parent.Parent);
            await Playlist.Update();
            Frame.GoBack();
        }

        private void DeleteThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Playlist.Parent.Remove(Playlist);

            Frame.GoBack();
        }

        private void OnSelectedSongChangedManually(object sender, SelectedSongChangedManuallyEventArgs e)
        {
            try
            {
                Playlist.Parent.Parent.CurrentPlaylist = Playlist;
            }
            catch (System.Exception exc)
            {
                MobileDebug.Service.WriteEventPair("OnSelectedSongChangedManuallyFail", exc,
                    "CurrentPlaylist: ", Playlist?.Parent?.Parent?.CurrentPlaylist);
            }

            Frame.GoBack();
        }
    }
}
