using FolderMusic.ViewModels;
using MusicPlayer;
using MusicPlayer.Data;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace FolderMusic
{
    public sealed partial class PlaylistPage : Page
    {
        private PlaylistViewModel viewModel;

        public PlaylistPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = viewModel = new PlaylistViewModel(e.Parameter as IPlaylist);
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
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await viewModel.Base.Reset(stopToken);
            Frame.GoBack();

            if (viewModel.Base.Songs.Count == 0) Frame.GoBack();
        }

        private async void SearchForNewSongs_Click(object sender, RoutedEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await viewModel.Base.AddNew(stopToken);
            Frame.GoBack();
        }

        private async void UpdateThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await viewModel.Base.Update(stopToken);
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
