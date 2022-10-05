using MusicPlayer;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using MusicPlayer.Handler;
using Windows.UI.Popups;
using FolderMusic.NavigationParameter;
using MusicPlayer.Models.Foreground.Interfaces;
using MusicPlayer.UpdateLibrary;

namespace FolderMusic
{
    public sealed partial class MainPage : Page
    {
        private const double timeOffsetFactor = 20;

        private bool loopImageEntered = false, shuffleImageEntered = false;
        private ForegroundPlayerHandler handler;
        private ILibrary library;

        private SongsView currentPlaylistSongListView;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = handler = (ForegroundPlayerHandler)e.Parameter;
            library = handler?.Library;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (library.Playlists.Count > 0) return;

            ParentUpdateProgress progress;
            Task task = library.Update(out progress);
            Frame.Navigate(typeof(UpdateProgressPage), progress);
            await task;
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                MobileDebug.Service.WriteEvent("LoopTapped");
                handler.CurrentPlaylist.Songs.SetNextShuffle(handler.CurrentSong);
            }
            catch (Exception exc)
            {
                MobileDebug.Service.WriteEvent("Shuffle_Tapped", exc, handler?.CurrentPlaylist?.Songs?.Shuffle.Type);
            }
        }

        private void ShuffleImage_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            shuffleImageEntered = true;
        }

        private void ShuffleImage_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!shuffleImageEntered) return;

            shuffleImageEntered = false;
            sbdShuffleImageTap.Begin();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            handler.Previous();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (handler.IsPlaying) handler.Pause();
            else handler.Play();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            handler.Next();
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            library.CurrentPlaylist.SetNextLoop();
        }

        private void LoopImage_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            loopImageEntered = true;
        }

        private void LoopImage_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!loopImageEntered) return;

            loopImageEntered = false;
            sbdLoopImageTap.Begin();
        }

        private async void PlaylistsView_UpdateSongsClick(object sender, PlaylistActionEventArgs e)
        {
            ChildUpdateProgress progress;
            Task task = e.Playlist.Update(out progress);
            Frame.Navigate(typeof(UpdateProgressPage), progress);
            await task;
        }

        private async void PlaylistsView_UpdateFilesClick(object sender, PlaylistActionEventArgs e)
        {
            ChildUpdateProgress progress;
            Task task = e.Playlist.UpdateFast(out progress);
            Frame.Navigate(typeof(UpdateProgressPage), progress);
            await task;
        }

        private void PlaylistsView_PlayClick(object sender, PlaylistActionEventArgs e)
        {
            library.CurrentPlaylist = e.Playlist;
            handler.Play();
        }

        private void PlaylistsView_DetailsClick(object sender, PlaylistActionEventArgs e)
        {
            PlaylistPageParameter parameter = new PlaylistPageParameter(handler, e.Playlist);
            bool navigated = Frame.Navigate(typeof(PlaylistPage), parameter);
        }

        private async void AbbUpdateAll_Click(object sender, RoutedEventArgs e)
        {
            ParentUpdateProgress progress;
            Task task = library.Update(out progress);
            Frame.Navigate(typeof(UpdateProgressPage), progress);
            await task;
        }

        private async void AbbUpdateFolders_Click(object sender, RoutedEventArgs e)
        {
            ParentUpdateProgress progress;
            Task task = library.Update(true, out progress);
            Frame.Navigate(typeof(UpdateProgressPage), progress);
            await task;
        }

        private void CurrentSong_Tapped(object sender, TappedRoutedEventArgs e)
        {
            currentPlaylistSongListView?.ScrollToCurrentSong();
        }

        private void SongListView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            currentPlaylistSongListView = sender as SongsView;
        }

        private async void AbbDebugSite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Frame.Navigate(typeof(MobileDebug.DebugPage));
            }
            catch (Exception exc)
            {
                await new Windows.UI.Popups.MessageDialog(exc.Message, e.GetType().Name).ShowAsync();
            }
        }

        private void Hub_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(LockPage));
        }

        private void CurrentSong_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (library.CurrentPlaylist == null)
            {
                return;
            }

            double cumX = e.Cumulative.Translation.X;
            double cumY = e.Cumulative.Translation.Y;
            int totalSeconds = (int)(cumX / timeOffsetFactor);
            int seconds = totalSeconds % 60;
            int minutes = (totalSeconds - seconds) / 60;

            tblTimeOffset.Text = string.Format("{0}{1}:{2:00}",
                cumX < 0 ? "-" : "", Math.Abs(minutes), Math.Abs(seconds));
            gidTimeOffset.Visibility = Math.Abs(cumY) < Math.Abs(cumX) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CurrentSong_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (library.CurrentPlaylist == null)
            {
                return;
            }

            double cumX = e.Cumulative.Translation.X;
            double cumY = e.Cumulative.Translation.Y;
            double totalSeconds = cumX / timeOffsetFactor;

            if (Math.Abs(cumY) < Math.Abs(cumX))
            {
                double ratioDelta = totalSeconds / library.CurrentPlaylist.CurrentSong.Duration.TotalSeconds;
                handler.PositionRatio += ratioDelta;
            }

            gidTimeOffset.Visibility = Visibility.Collapsed;
        }

        private async void AbbTest1_Click(object sender, RoutedEventArgs e)
        {
            await new MessageDialog(library.Playlists.Count.ToString()).ShowAsync();
        }
    }
}
