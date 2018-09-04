using System;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System.Linq;
using MusicPlayer.Data;
using MusicPlayer;

namespace FolderMusic
{
    public sealed partial class MainPage : Page
    {
        private bool loopImageEntered = false, shuffleImageEntered = false;
        private ILibrary library;
        private ViewModel viewModel;

        private SongsView currentPlaylistSongListView;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            library = Library.Load(true);
            viewModel = new ViewModel(library);

            DataContext = viewModel;

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //if (library.SkippedSongs.MoveNext()) Frame.Navigate(typeof(SkipSongsPage));
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (Frame.BackStackDepth == 0) Application.Current.Exit();
            else
            {
                Frame.GoBack();
                e.Handled = true;
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
            MobileDebug.Manager.WriteEvent("PlayPauseButtonClick", library.IsPlaying);
            library.IsPlaying = !library.IsPlaying;

            //if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing) library.IsPlaying = true;
            //else library.IsPlaying = false;
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

        private void Playlist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            library.CurrentPlaylist = (sender as Grid).DataContext as IPlaylist;
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            IPlaylist playlist = (sender as Image).DataContext as IPlaylist;

            library.CurrentPlaylist = playlist;
            library.IsPlaying = true;
        }

        private void DetailPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!PlaylistPage.Open) Frame.Navigate(typeof(PlaylistPage), (sender as Image).DataContext);
        }

        private void PlaylistsPlaylist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            library.CurrentPlaylist.Songs.Remove(song);
        }

        private async void RefreshPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), library);

            IPlaylist playlist = (sender as MenuFlyoutItem).DataContext as IPlaylist;
            await playlist.Refresh();

            Frame.GoBack();
        }

        private async void UpdatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), library);

            IPlaylist playlist = (sender as MenuFlyoutItem).DataContext as IPlaylist;
            await playlist.Update();

            Frame.GoBack();
        }

        private async void SearchForNewSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), library);

            IPlaylist playlist = (sender as MenuFlyoutItem).DataContext as IPlaylist;
            await playlist.AddNew();

            Frame.GoBack();
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = (sender as MenuFlyoutItem).DataContext as IPlaylist;

            library.Playlists.Remove(playlist);
        }

        private async void ResetLibraryFromStorage_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), library);
            await library.Refresh();
            Frame.GoBack();
        }

        private async void UpdateExistingPlaylists_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoadingPage), library);
            await library.Update();
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

        private async void TestFunktion_Click(object sender, RoutedEventArgs e)
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

        private async void TestFunktion_Click2(object sender, RoutedEventArgs e)
        {
            StorageFile file = await library.CurrentPlaylist.CurrentSong.GetStorageFileAsync();
            var v = await file.Properties.GetMusicPropertiesAsync();
            string text = string.Empty;

            text += "Album: " + v.Album + "\n";
            text += "AlbumArtist: " + v.AlbumArtist + "\n";
            text += "Artist: " + v.Artist + "\n";
            text += "Bitrate: " + v.Bitrate.ToString() + "\n";
            text += "Composers: " + string.Join("; ", v.Composers) + "\n";
            text += "Conductors: " + string.Join("; ", v.Conductors) + "\n";
            text += "Duration: " + v.Duration.TotalSeconds.ToString() + "\n";
            text += "Genre: " + string.Join("; ", v.Genre) + "\n";
            text += "Producers: " + string.Join("; ", v.Producers) + "\n";
            text += "Publisher: " + v.Publisher + "\n";
            text += "Rating: " + v.Rating.ToString() + "\n";
            text += "Subtitle: " + v.Subtitle + "\n";
            text += "Title: " + v.Title + "\n";
            text += "Writers: " + string.Join("; ", v.Writers) + "\n";
            text += "Year: " + v.Year.ToString();

            await new Windows.UI.Popups.MessageDialog(text).ShowAsync();
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            string[] filenames = new string[] { "TaskCompleted.txt", "OnCanceled.txt", "Activated.txt", "Closed.txt" };
            long taskCompletedTicks = 0, onCanceledTicks = 0;
            string message, taskCompletedPath, onCanceledPath, taskCompletedTime, onCanceledTime;

            taskCompletedPath = ApplicationData.Current.LocalFolder.Path + "\\TaskCompleted.txt";
            onCanceledPath = ApplicationData.Current.LocalFolder.Path + "\\OnCanceled.txt";

            try
            {
                string ticksText = await PathIO.ReadTextAsync(taskCompletedPath);
                taskCompletedTicks = long.Parse(ticksText);
            }
            catch { }

            try
            {
                string ticksText = await PathIO.ReadTextAsync(onCanceledPath);
                onCanceledTicks = long.Parse(ticksText);
            }
            catch { }

            System.Diagnostics.Debug.WriteLine("Task: {0}\nOn: {1}", taskCompletedTicks, onCanceledTicks);

            taskCompletedTime = MobileDebug.Event.GetDateTimeString(taskCompletedTicks);
            onCanceledTime = MobileDebug.Event.GetDateTimeString(onCanceledTicks);

            message = string.Format("TaskCompleted:\n{0}\n\nOnCanceled:\n{1}", taskCompletedTime, onCanceledTime);

            await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        }

        private async Task<string> GetDebugText(string filename)
        {
            try
            {
                string ticksText = await PathIO.ReadTextAsync(ApplicationData.Current.LocalFolder.Path + "\\" + filename);

                long ticks = long.Parse(ticksText);
                string timeText = MobileDebug.Event.GetDateTimeString(ticks);

                return string.Format("{0}:\n{1}", filename, timeText);
            }
            catch { }

            return string.Empty;
        }

        private void TestFunktion_Click3(object sender, RoutedEventArgs e)
        {
            //await new MessageDialog("CurrentSongPosition").ShowAsync();
            //try
            //{
            //    IPlaylist playlist = null;
            //    TimeSpan position = playlist.GetCurrentSongPosition();
            //    await new MessageDialog("CurrentSongPosition: " + position.TotalSeconds).ShowAsync();
            //}
            //catch (Exception exc)
            //{
            //    await new MessageDialog("CurrentSongPositionFail:\n" + exc.Message).ShowAsync();
            //}
        }

        private void TestFunktion_Click4(object sender, RoutedEventArgs e)
        {
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
                MobileDebug.Manager.WriteEvent("IOLoadTextFail", e, filenameWithExtention);
            }

            return string.Empty;
        }
    }
}
