using MusicPlayer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using FolderMusic.EventArgs;
using FolderMusic.NavigationParameter;
using MusicPlayer.Handler;
using MusicPlayer.UpdateLibrary;
using System.Threading.Tasks;
using MusicPlayer.Models.Foreground.Interfaces;

namespace FolderMusic
{
    public sealed partial class PlaylistPage : Page
    {
        private ForegroundPlayerHandler handler;
        private IPlaylist playlist;

        public PlaylistPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            PlaylistPageParameter parameter = (PlaylistPageParameter)e.Parameter;
            handler = parameter?.Handler;
            DataContext = playlist = parameter?.Playlist;
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            playlist.Songs.SetNextShuffle(playlist.CurrentSong);
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            playlist.SetNextLoop();
        }

        private async void ResetThisPlaylist_Click(object sender, RoutedEventArgs e)
        {
            ChildUpdateProgress progress;
            Task task = playlist.Update(out progress);
            Frame.Navigate(typeof(UpdateProgressPage), progress);
            await task;

            if (playlist.Songs.Count == 0) Frame.GoBack();
        }

        private async void SearchForNewSongs_Click(object sender, RoutedEventArgs e)
        {
            ChildUpdateProgress progress;
            Task task = playlist.UpdateFast(out progress);
            Frame.Navigate(typeof(UpdateProgressPage), progress);
            await task;

            if (playlist.Songs.Count == 0) Frame.GoBack();
        }

        private void OnSelectedSongChangedManually(object sender, SelectedSongChangedManuallyEventArgs e)
        {
            try
            {
                handler.Library.CurrentPlaylist = playlist;
            }
            catch (System.Exception exc)
            {
                MobileDebug.Service.WriteEventPair("OnSelectedSongChangedManuallyFail",
                    "CurrentPlaylist", handler?.Library?.CurrentPlaylist, exc);
            }

            Frame.GoBack();
        }
    }
}
