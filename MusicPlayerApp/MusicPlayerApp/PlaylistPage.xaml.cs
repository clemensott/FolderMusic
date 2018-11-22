using FolderMusic.ViewModels;
using MusicPlayer;
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

        public PlaylistPage()
        {
            this.InitializeComponent();
            playlistPageOpen = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = viewModel = e.Parameter as PlaylistViewModel;
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
            viewModel.Base.Songs.SetNextShuffle();
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            viewModel.Base.SetNextLoop();
        }

        private async void ResetThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), viewModel.Base.Parent.Parent);
            await viewModel.Base.Reset();
            Frame.GoBack();

            if (viewModel.Base.Songs.Count == 0) Frame.GoBack();
        }

        private async void SearchForNewSongs_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), viewModel.Base.Parent.Parent);
            await viewModel.Base.AddNew();
            Frame.GoBack();
        }

        private async void UpdateThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), viewModel.Base.Parent.Parent);
            await viewModel.Base.Update();
            Frame.GoBack();
        }

        private void DeleteThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Base.Parent.Remove(viewModel.Base);

            Frame.GoBack();
        }

        private void OnSelectedSongChangedManually(object sender, SelectedSongChangedManuallyEventArgs e)
        {
            try
            {
                viewModel.Base.Parent.Parent.CurrentPlaylist = viewModel.Base;
            }
            catch (System.Exception exc)
            {
                MobileDebug.Service.WriteEventPair("OnSelectedSongChangedManuallyFail", exc,
                    "CurrentPlaylist: ", viewModel.Base?.Parent?.Parent?.CurrentPlaylist);
            }

            Frame.GoBack();
        }
    }
}
