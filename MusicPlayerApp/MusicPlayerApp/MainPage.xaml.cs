using FolderMusic.ViewModels;
using MusicPlayer;
using MusicPlayer.Data;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Popups;
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
        private MainViewModel viewModel;

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
                viewModel = new MainViewModel(library);
                DataContext = viewModel;

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

        private async void SkippedSongs_SkippedSong(object sender, EventArgs e)
        {
            //if (!SkipSongsPage.Open && await sender.HasSongs()) Frame.Navigate(typeof(SkipSongsPage), sender);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!checkedSkippedSongs && await library.SkippedSongs.HasSongs())
            {
                checkedSkippedSongs = true;
                Frame.Navigate(typeof(SkipSongsPage), library.SkippedSongs);
            }
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            library.CurrentPlaylist.Songs.SetNextShuffle();
        }

        private void ShuffleImage_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            shuffleImageEntered = true;
        }

        private void ShuffleImage_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!shuffleImageEntered) return;

            shuffleImageEntered = false;
            ShuffleImageTap.Begin();
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
            LoopImageTap.Begin();
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

        private async void AbbComReset_Click(object sender, RoutedEventArgs e)
        {
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
