using MusicPlayer.Data;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Core;
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
        private ViewModel viewModel;

        private SongsView currentPlaylistSongListView;

        public async static void DoSafe(DispatchedHandler handler)
        {
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess) handler();
            else await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, handler);
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            checkedSkippedSongs = false;
            library = Library.LoadSimple(true);
            viewModel = new ViewModel(library);

            DataContext = viewModel;

            library.LibraryChanged += Library_LibraryChanged;
            library.SkippedSongs.SkippedSong += SkippedSongs_SkippedSong;
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        private async void Library_LibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            if (args.NewPlaylists.Count > 0) return;

            Frame.Navigate(typeof(LoadingPage), library);
            await library.Reset();
            Frame.GoBack();
        }

        private void SkippedSongs_SkippedSong(SkipSongs sender)
        {
            if (!SkipSongsPage.Open && sender.HasSongs()) Frame.Navigate(typeof(SkipSongsPage), sender);
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (!Frame.CanGoBack) Application.Current.Exit();
            else
            {
                Frame.GoBack();
                e.Handled = true;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!checkedSkippedSongs && library.SkippedSongs.HasSongs())
            {
                checkedSkippedSongs = true;
                Frame.Navigate(typeof(SkipSongsPage), library.SkippedSongs);
            }
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            library.CurrentPlaylist.SetNextShuffle();
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
            library.CurrentPlaylist.SetPreviousSong();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            library.IsPlaying = !library.IsPlaying;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            library.CurrentPlaylist.SetNextSong();
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
            Frame.Navigate(typeof(LoadingPage), library);
            await library.Reset();
            Frame.GoBack();
        }

        private async void UpdateExistingPlaylists_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), library);
            await library.Update();
            Frame.GoBack();
        }

        private async void ResetAllSongs_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), library);
            await library.ResetSongs();
            Frame.GoBack();
        }

        private async void AddNotExistingPlaylists_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), library);
            await library.AddNew();
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
                await new Windows.UI.Popups.MessageDialog(exc.Message).ShowAsync();
            }
        }

        private void AbbComPing_Click(object sender, RoutedEventArgs e)
        {
            MusicPlayer.Communication.BackForegroundCommunicator.instance?.Ping();
        }

        private async void AbbComReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await MusicPlayer.Communication.BackForegroundCommunicator.Reset();
            }
            catch { }
        }

        private async void hub_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            await new Windows.UI.Popups.MessageDialog("Lock").ShowAsync();
        }

        private void AbbTest1_Click(object sender, RoutedEventArgs e)
        {
            Library.CheckLibrary(library);
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
