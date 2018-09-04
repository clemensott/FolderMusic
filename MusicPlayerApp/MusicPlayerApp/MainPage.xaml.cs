using LibraryLib;
using FolderMusicLib;
using PlayerIcons;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicPlayerApp
{
    public sealed partial class MainPage : Page
    {
        private bool pauseClick = false, loopImageEntered = false, shuffleImageEntered = false;
        private Timer timer;

        private ListBox lbxCurrentPlaylist;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            Icons.Theme = RequestedTheme = (Background as SolidColorBrush).Color.B == 0 ? ElementTheme.Dark : ElementTheme.Light;

            DataContext = ViewModel.Current;
            timer = new Timer(new TimerCallback(UpdateSongPosition), new object(), Timeout.Infinite, 1000);

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Window.Current.Activated += Current_Activated;
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            Library.Current.ScrollToIndex += Library_ScroolToIndex;

            System.Diagnostics.Debug.WriteLine("MainPageConstructor");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Icons.Theme = RequestedTheme;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Library_ScroolToIndex(Library.Current, Library.Current.CurrentPlaylist);

            ViewModel.Current.SetMainPageLoaded();
            SkipSongsPage.NavigateToIfSkipSongsExists();

            GetLibraryData();
        }

        private async void GetLibraryData()
        {
            while (!Library.IsLoaded)
            {
                BackgroundMediaPlayer.Current.Pause();
                BackgroundCommunicator.SendGetXmlText();
                await Task.Delay(5000);
            }
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            try
            {
                if (e.WindowActivationState == CoreWindowActivationState.Deactivated) ChangeTimer(false);
                else if (e.WindowActivationState == CoreWindowActivationState.CodeActivated &&
                    BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing) ChangeTimer(true);
            }
            catch { }
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;

            if (SkipSongsPage.Open) SkipSongsPage.GoBack();
            else if (LoadingPage.Open) LoadingPage.GoBack();
            else if (PlaylistPage.Open) PlaylistPage.GoBack();
            else Application.Current.Exit();
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing) ChangeTimer(true);
            else if (pauseClick && sender.CurrentState == MediaPlayerState.Paused) pauseClick = false;
            else return;

            ViewModel.Current.UpdatePlayPauseIconAndText();
        }

        public void ChangeTimer(bool activate)
        {
            int timeOut = activate ? 1000 - BackgroundMediaPlayer.Current.Position.Milliseconds : Timeout.Infinite;
            timer.Change(timeOut, 1000);

            if (activate) UpdateSongPosition(new object());
        }

        private void UpdateSongPosition(object state)
        {
            ViewModel.Current.ChangeSliderValue();

            if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing) ChangeTimer(false);
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
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
            {
                ViewModel.Current.Pause();
                pauseClick = true;
            }
            else ViewModel.Current.Play();
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
            if (!PlaylistPage.Open) Frame.Navigate(typeof(PlaylistPage));
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Playlist playlist = (sender as Image).DataContext as Playlist;

            if (playlist.IsEmptyOrLoading) return;

            Library.Current.CurrentPlaylist = playlist;
            ViewModel.Current.Play();
        }

        private void PlaylistsPlaylist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (((sender as Grid).DataContext as Playlist).IsEmptyOrLoading) return;

            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void CurrentPlaylistSong_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (((sender as Grid).DataContext as Song).IsEmptyOrLoading) return;

            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void RefreshSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            await song.Refresh();
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;
            int songsIndex = Library.Current.CurrentPlaylist.Songs.IndexOf(song);

            Library.Current.CurrentPlaylist.RemoveSong(songsIndex);
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

            Library.Current.Delete(playlist);
        }

        private void sld_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.Current.EnteredSlider();
        }

        private void Page_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.Current.SetSliderValue();
        }

        private async void ResetLibraryFromStorage_Click(object sender, RoutedEventArgs e)
        {
            await LoadingPage.NavigateTo();
            await Library.Current.ResetLibraryFromStorage();
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

        private void lbxCurrentPlaylist_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ListBox lbx = sender as ListBox;
            lbxCurrentPlaylist = lbx;

            if (lbx == null || !lbx.Items.Contains(ViewModel.Current.CurrentPlaylist.CurrentSong)) return;

            lbx.ScrollIntoView(ViewModel.Current.CurrentPlaylist.CurrentSong);
        }

        private async void Library_ScroolToIndex(object sender, Playlist e)
        {
            if (lbxCurrentPlaylist == null || e.IsEmptyOrLoading) return;

            while (lbxCurrentPlaylist.Items.Count < e.ShuffleListIndex) await Task.Delay(10);

            lbxCurrentPlaylist.ScrollIntoView(lbxCurrentPlaylist.Items[e.ShuffleListIndex]);
        }

        private void CurrentSong_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Library_ScroolToIndex(Library.Current, Library.Current.CurrentPlaylist);
        }

        private void TestFunktion_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(TextPage));
        }

        private async void TestFunktion_Click2(object sender, RoutedEventArgs e)
        {
            StorageFile data = await ApplicationData.Current.LocalFolder.GetFileAsync("Data.xml");

            await data.CopyAsync(KnownFolders.PicturesLibrary);
        }

        private void TestFunktion_Click3(object sender, RoutedEventArgs e)
        {
            double beforeMilli, deltaMilli;

            beforeMilli = DateTime.Now.TimeOfDay.TotalMilliseconds;

            Playlist playlist = new Playlist(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist");

            playlist.Songs.AddRange(new System.Collections.Generic.List<Song>()
            { new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Ace of Base - All That She Wants.mp3"),
                new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Charli Xcx - Break The Rules.mp3"),
                new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Evanescence - My Immortal.mp3"),
                new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Kelly Clarkson - Heartbeat Song.mp3"),
                new Song(@"C:\Data\Users\Public\Music\Das Känguru\TestPlaylist\Ronald Bell - Celebration.mp3") });

            playlist.SetNextShuffle();

            Library.Current.Playlists.Add(playlist);
            Library.Current.Playlists = new System.Collections.Generic.List<Playlist>(Library.Current.Playlists);

            deltaMilli = DateTime.Now.TimeOfDay.TotalMilliseconds - beforeMilli;
            System.Diagnostics.Debug.WriteLine("deltaMilli: " + deltaMilli.ToString());
        }

        private async void TestFunktion_Click4(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = await PathIO.ReadTextAsync(ApplicationData.Current.LocalFolder.Path + 
                    "\\" + "LibraryDeletedDelete.txt");

                await new Windows.UI.Popups.MessageDialog(text).ShowAsync();
            }
            catch { }
        }
    }
}
