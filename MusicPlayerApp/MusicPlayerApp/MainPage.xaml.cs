using MusicPlayer;
using MusicPlayer.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace FolderMusic
{
    public sealed partial class MainPage : Page
    {
        private bool checkedSkippedSongs, loopImageEntered = false, shuffleImageEntered = false;
        private ILibrary library;

        private SongsView currentPlaylistSongListView;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            checkedSkippedSongs = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if ((ILibrary)e.Parameter != library)
            {
                library = (ILibrary)e.Parameter;
                //viewModel = new MainViewModel(library);
                //DataContext = viewModel;
                DataContext = library;

                library.Loaded += Library_Loaded;
            }
        }

        private async void Library_Loaded(object sender, EventArgs args)
        {
            if (library.Playlists.Count > 0) return;

            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await library.Reset(stopToken);
            Frame.GoBack();

            AutoSaveLoad.CheckLibrary(library, "ResetedOnLoaded");
        }

        private void SkippedSongs_SkippedSong(object sender, EventArgs e)
        {
            //if (!SkipSongsPage.Open && await sender.HasSongs()) Frame.Navigate(typeof(SkipSongsPage), sender);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //if (!checkedSkippedSongs && await library.SkippedSongs.HasSongs())
            //{
            //    checkedSkippedSongs = true;
            //    Frame.Navigate(typeof(SkipSongsPage), library.SkippedSongs);
            //}
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                library.CurrentPlaylist.Songs.SetNextShuffle();
            }
            catch (Exception exc)
            {
                MobileDebug.Service.WriteEvent("Shuffle_Tapped", exc, library?.CurrentPlaylist?.Songs?.Shuffle.Type);
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
            library.CurrentPlaylist.ChangeCurrentSong(-1);
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            library.IsPlaying = !library.IsPlaying;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            library.CurrentPlaylist.ChangeCurrentSong(1);
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

        private async void PlaylistsView_UpdateClick(object sender, PlaylistActionEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await e.Playlist.Reset(stopToken);
            if (!stopToken.IsStopped) Frame.GoBack();
        }

        private async void PlaylistsView_ResetClick(object sender, PlaylistActionEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await e.Playlist.Update(stopToken);
            if (!stopToken.IsStopped) Frame.GoBack();
        }

        private async void PlaylistsView_ResetSongsClick(object sender, PlaylistActionEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await e.Playlist.ResetSongs(stopToken);
            if (!stopToken.IsStopped) Frame.GoBack();
        }

        private async void PlaylistsView_AddNewClick(object sender, PlaylistActionEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await e.Playlist.AddNew(stopToken);
            if (!stopToken.IsStopped) Frame.GoBack();
        }

        private void PlaylistsView_RemoveClick(object sender, PlaylistActionEventArgs e)
        {
            e.Playlist.Parent.Remove(e.Playlist);
        }

        private void PlaylistsView_PlayClick(object sender, PlaylistActionEventArgs e)
        {
            e.Playlist.Parent.Parent.CurrentPlaylist = e.Playlist;
            e.Playlist.Parent.Parent.IsPlaying = true;
        }

        private void PlaylistsView_DetailsClick(object sender, PlaylistActionEventArgs e)
        {
            bool navigated = Frame.Navigate(typeof(PlaylistPage), e.Playlist);
            MobileDebug.Service.WriteEvent("ImgDetailTapped2", e.Playlist?.AbsolutePath, navigated);
        }

        private async void ResetLibraryFromStorage_Click(object sender, RoutedEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await library.Reset(stopToken);
            Frame.GoBack();
        }

        private async void UpdateExistingPlaylists_Click(object sender, RoutedEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await library.Update(stopToken);
            Frame.GoBack();
        }

        private async void ResetAllSongs_Click(object sender, RoutedEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await library.ResetSongs(stopToken);
            Frame.GoBack();
        }

        private async void AddNotExistingPlaylists_Click(object sender, RoutedEventArgs e)
        {
            StopOperationToken stopToken = new StopOperationToken();

            Frame.Navigate(typeof(LoadingPage), stopToken);
            await library.AddNew(stopToken);
            Frame.GoBack();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
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

        private async void AbbComPing_Click(object sender, RoutedEventArgs e)
        {
            StorageFile file = await KnownFolders.VideosLibrary.CreateFileAsync("Data.xml", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, XmlConverter.Serialize(library));
        }

        private void AbbComReset_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = library.Playlists.First();
            bool navigated = Frame.Navigate(typeof(PlaylistPage), playlist);

            MobileDebug.Service.WriteEvent("NavigateToPlaylistPage", playlist.AbsolutePath, navigated);
        }

        private void hub_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(LockPage));
        }

        private void AbbTest1_Click(object sender, RoutedEventArgs e)
        {
            AutoSaveLoad.CheckLibrary(library, "Abb");
        }

        public static string LoadText(string filenameWithExtention)
        {
            try
            {
                string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;
                Task<string> load = PathIO.ReadTextAsync(path).AsTask();
                load.Wait();

                return load.Result;
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("IOLoadTextFail", e, filenameWithExtention);
            }

            return null;
        }
    }
}
