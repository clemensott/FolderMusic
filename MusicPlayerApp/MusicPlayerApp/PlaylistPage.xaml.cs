using MusicPlayer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using MusicPlayer.Models.Interfaces;

namespace FolderMusic
{
    public sealed partial class PlaylistPage : Page
    {
        private IPlaylist viewModel;

        public PlaylistPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = viewModel = e.Parameter as IPlaylist;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            viewModel.Songs.SetNextShuffle();
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            viewModel.SetNextLoop();
        }

        private async void ResetThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await viewModel.Reset(stopToken);
            Frame.GoBack();

            if (viewModel.Songs.Count == 0) Frame.GoBack();
        }

        private async void SearchForNewSongs_Click(object sender, RoutedEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await viewModel.AddNew(stopToken);
            Frame.GoBack();
        }

        private async void UpdateThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await viewModel.Update(stopToken);
            Frame.GoBack();
        }

        private void DeleteThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Parent.Remove(viewModel);

            Frame.GoBack();
        }

        private void OnSelectedSongChangedManually(object sender, SelectedSongChangedManuallyEventArgs e)
        {
            try
            {
                viewModel.Parent.Parent.CurrentPlaylist = viewModel;
            }
            catch (System.Exception exc)
            {
                MobileDebug.Service.WriteEventPair("OnSelectedSongChangedManuallyFail",
                    "CurrentPlaylist", viewModel?.Parent?.Parent?.CurrentPlaylist, exc);
            }

            Frame.GoBack();
        }
    }
}
