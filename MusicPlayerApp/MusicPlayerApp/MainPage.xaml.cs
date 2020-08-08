using MusicPlayer;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using MusicPlayer.Handler;
using Windows.UI.Popups;
using Windows.Media.Playback;
using FolderMusic.NavigationParameter;
using MusicPlayer.Models.Foreground.Interfaces;
using MusicPlayer.UpdateLibrary;

namespace FolderMusic
{
    public sealed partial class MainPage : Page
    {
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
            currentPlaylistSongListView?.ScrollToCurrentSongDirect();
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

        private void hub_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(LockPage));
        }

        private async void AbbTest1_Click(object sender, RoutedEventArgs e)
        {
            await new MessageDialog($"{handler.CurrentPlayerState} | {BackgroundMediaPlayer.Current.CurrentState}").ShowAsync();
        }
    }
}
