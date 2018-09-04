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

namespace FolderMusic
{
    public sealed partial class MainPage : Page
    {

        private bool loopImageEntered = false, shuffleImageEntered = false;
        private long loadedTicks;
        private PlayerPosition playerPosition;

        private SongListView currentPlaylistSongListView;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            DataContext = ViewModel.Current;

            playerPosition = new PlayerPosition();

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Window.Current.Activated += Window_Activated;
            Window.Current.Closed += Window_Closed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            loadedTicks = DateTime.Now.Ticks;

            if (Library.Current.SkippedSongs.MoveNext()) Frame.Navigate(typeof(SkipSongsPage));
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            try
            {
                if (e.WindowActivationState == CoreWindowActivationState.PointerActivated) return;

                bool isActiv = e.WindowActivationState == CoreWindowActivationState.CodeActivated;

                if (!isActiv) playerPosition.StopTimer();
                else if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing) playerPosition.StartTimer();
            }
            catch { }
        }

        private void Window_Closed(object sender, CoreWindowEventArgs e)
        {

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
            Library.Current.CurrentPlaylist.SetNextShuffle();
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
            Library.Current.CurrentPlaylist.SetPreviousSong();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            Library.Current.IsPlaying = !Library.Current.IsPlaying;

            //if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing) Library.Current.IsPlaying = true;
            //else Library.Current.IsPlaying = false;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            Library.Current.CurrentPlaylist.SetNextSong();
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Library.Current.CurrentPlaylist.SetNextLoop();
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
            Library.Current.CurrentPlaylist = (sender as Grid).DataContext as Playlist;
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Playlist playlist = (sender as Image).DataContext as Playlist;

            if (playlist.IsEmptyOrLoading) return;

            Library.Current.CurrentPlaylist = playlist;
            Library.Current.IsPlaying = true;
        }

        private void DetailPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!PlaylistPage.Open) Frame.Navigate(typeof(PlaylistPage), (sender as Image).DataContext);
        }

        private void PlaylistsPlaylist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (((sender as Grid).DataContext as Playlist).IsEmptyOrLoading) return;

            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            Library.Current.CurrentPlaylist.Songs.Remove(song);
        }

        private async void RefreshPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;

            await LoadingPage.NavigateTo();
            await playlist.LoadSongsFromStorage();

            LoadingPage.GoBack();
        }

        private async void UpdatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;

            await LoadingPage.NavigateTo();
            await playlist.UpdateSongsFromStorage();

            LoadingPage.GoBack();
        }

        private async void SearchForNewSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            await LoadingPage.NavigateTo();

            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;
            await playlist.SearchForNewSongs();

            LoadingPage.GoBack();
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;

            Library.Current.Playlists.Remove(playlist);
        }

        private void sld_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.Current.PlayerPositionEnabled = false;
        }

        private void Page_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.Current.PlayerPositionEnabled = true;
        }

        private async void ResetLibraryFromStorage_Click(object sender, RoutedEventArgs e)
        {
            await LoadingPage.NavigateTo();
            await Library.Current.RefreshLibraryFromStorage();
            LoadingPage.GoBack();
        }

        private async void UpdateExistingPlaylists_Click(object sender, RoutedEventArgs e)
        {
            await LoadingPage.NavigateTo();
            await Library.Current.UpdateExistingPlaylists();
            LoadingPage.GoBack();
        }

        private async void AddNotExistingPlaylists_Click(object sender, RoutedEventArgs e)
        {
            await LoadingPage.NavigateTo();
            await Library.Current.AddNotExistingPlaylists();
            LoadingPage.GoBack();
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
            currentPlaylistSongListView = sender as SongListView;
        }

        private void TestFunktion_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(FolderMusicDebug.DebugPage));
        }

        private async void TestFunktion_Click2(object sender, RoutedEventArgs e)
        {
            StorageFile file = await Library.Current.CurrentPlaylist.CurrentSong.GetStorageFileAsync();
            var v = await file.Properties.GetMusicPropertiesAsync();
            string text = string.Empty;

            text += "Album: " + v.Album + "\n";
            text += "AlbumArtist: " + v.AlbumArtist + "\n";
            text += "Artist: " + v.Artist + "\n";
            text += "Bitrate: " + v.Bitrate.ToString() + "\n";

            text += "Composers: ";
            foreach (string composer in v.Composers) text += composer + "; ";
            text += "\n";

            text += "Conductors: ";
            foreach (string conductors in v.Conductors) text += conductors + "; ";
            text += "\n";

            text += "Duration: " + v.Duration.TotalSeconds.ToString() + "\n";

            text += "Genre: ";
            foreach (string genre in v.Genre) text += genre + "; ";
            text += "\n";

            text += "Producers: ";
            foreach (string producers in v.Producers) text += producers + "; ";
            text += "\n";

            text += "Publisher: " + v.Publisher + "\n";
            text += "Rating: " + v.Rating.ToString() + "\n";
            text += "Subtitle: " + v.Subtitle + "\n";
            text += "Title: " + v.Title + "\n";

            text += "Writers: ";
            foreach (string writers in v.Writers) text += writers + "; ";
            text += "\n";

            text += "Year: " + v.Year.ToString() + "\n";

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

            taskCompletedTime = FolderMusicDebug.DebugEvent.GetDateTimeString(taskCompletedTicks);
            onCanceledTime = FolderMusicDebug.DebugEvent.GetDateTimeString(onCanceledTicks);

            message = string.Format("TaskCompleted:\n{0}\n\nOnCanceled:\n{1}", taskCompletedTime, onCanceledTime);

            await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        }

        private async Task<string> GetDebugText(string filename)
        {
            try
            {
                string ticksText = await PathIO.ReadTextAsync(ApplicationData.Current.LocalFolder.Path + "\\" + filename);

                long ticks = long.Parse(ticksText);
                string timeText = FolderMusicDebug.DebugEvent.GetDateTimeString(ticks);

                return string.Format("{0}:\n{1}", filename, timeText);
            }
            catch { }

            return string.Empty;
        }

        private void TestFunktion_Click3(object sender, RoutedEventArgs e)
        {
            double beforeMilli, deltaMilli;

            beforeMilli = DateTime.Now.TimeOfDay.TotalMilliseconds;

            Playlist playlist = new Playlist(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist");

            playlist.Songs.Add(new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Ace of Base - All That She Wants.mp3"));
            playlist.Songs.Add(new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Charli Xcx - Break The Rules.mp3"));
            playlist.Songs.Add(new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Evanescence - My Immortal.mp3"));
            playlist.Songs.Add(new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Kelly Clarkson - Heartbeat Song.mp3"));
            playlist.Songs.Add(new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Ronald Bell - Celebration.mp3"));

            playlist.SetNextShuffle();

            Library.Current.Playlists.Add(playlist);

            deltaMilli = DateTime.Now.TimeOfDay.TotalMilliseconds - beforeMilli;
            System.Diagnostics.Debug.WriteLine("deltaMilli: " + deltaMilli.ToString());
        }

        private void TestFunktion_Click4(object sender, RoutedEventArgs e)
        {
            try
            {
                Windows.Foundation.Collections.ValueSet vs = new Windows.Foundation.Collections.ValueSet();
                vs.Add("TestPrimary", typeof(int));
                new Windows.UI.Popups.MessageDialog("Success").ShowAsync();
            }
            catch (Exception exc)
            {
                new Windows.UI.Popups.MessageDialog("Fail\n" + exc.GetType() + "\n" + exc.Message).ShowAsync();
            }
        }
    }
}
