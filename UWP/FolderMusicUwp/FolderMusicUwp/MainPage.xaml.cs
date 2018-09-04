using FolderMusicUwpLib;
using LibraryLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Vorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 dokumentiert.

namespace FolderMusicUwp
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool pauseClick = false, loopImageEntered = false, shuffleImageEntered = false;
        private long loadedTicks;
        private Timer timer;

        private ListBox lbxCurrentPlaylist;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            Icons.Theme = RequestedTheme = (Background as SolidColorBrush).Color.B == 0 ? ElementTheme.Dark : ElementTheme.Light;

            DataContext = ViewModel.Current;
            timer = new Timer(new TimerCallback(UpdateSongPosition), new object(), Timeout.Infinite, 1000);

            //HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Window.Current.Activated += Window_Activated;
            //BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            //BackgroundMediaPlayer.Current.MediaOpened += BackgroundMediaPlayer_MediaOpened;
            Library.Current.ScrollToIndex += Library_ScrollToIndex;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Icons.Theme = RequestedTheme;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            loadedTicks = DateTime.Now.Ticks;
            Library_ScrollToIndex(Library.Current, Library.Current.CurrentPlaylist);

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

        private void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            try
            {
                if (e.WindowActivationState == CoreWindowActivationState.PointerActivated) return;

                bool isActiv = e.WindowActivationState == CoreWindowActivationState.CodeActivated;

                if (!isActiv) ChangeTimer(false);
                else if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing) ChangeTimer(true);

                BackgroundCommunicator.SendIsWindowActiv(isActiv);
                SaveTextClass.SaveText(e.WindowActivationState.ToString());
            }
            catch { }
        }
        /*
        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;

            if (SkipSongsPage.Open) SkipSongsPage.GoBack();
            else if (LoadingPage.Open) LoadingPage.GoBack();
            else if (PlaylistPage.Open) PlaylistPage.GoBack();
            else Application.Current.Exit();
        }                                           //          */

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing) ChangeTimer(true);
            else if (pauseClick && sender.CurrentState == MediaPlayerState.Paused) pauseClick = false;
            else return;

            ViewModel.Current.UpdatePlayPauseIconAndText();
        }

        private void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            ViewModel.Current.ChangeSliderValue();
        }

        public void ChangeTimer(bool activate)
        {
            int timeOut = activate ? 1000 - BackgroundMediaPlayer.Current.Position.Milliseconds : Timeout.Infinite;
            timer.Change(timeOut, 1000);

            if (activate) UpdateSongPosition(0);
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
            BackgroundCommunicator.SendPause();
            Library.Current.CurrentPlaylist = (sender as Grid).DataContext as Playlist;
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Playlist playlist = (sender as Image).DataContext as Playlist;

            if (playlist.IsEmptyOrLoading) return;

            Library.Current.CurrentPlaylist = playlist;
            ViewModel.Current.Play();
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

        private void CurrentPlaylistSong_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (((sender as Grid).DataContext as Song).IsEmptyOrLoading) return;

            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void RefreshSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            song.Refresh();
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

        private void lbxCurrentPlaylist_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (loadedTicks + 10000000 < DateTime.Now.Ticks) return;

            Library_ScrollToIndex(Library.Current, Library.Current.CurrentPlaylist);
        }

        private async void Library_ScrollToIndex(object sender, Playlist e)
        {
            if (lbxCurrentPlaylist == null || e.IsEmptyOrLoading) return;

            while (lbxCurrentPlaylist.Items.Count < e.ShuffleListIndex) await Task.Delay(10);

            lbxCurrentPlaylist.ScrollIntoView(lbxCurrentPlaylist.Items[e.ShuffleListIndex]);
        }

        private void CurrentSong_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Library_ScrollToIndex(Library.Current, Library.Current.CurrentPlaylist);
        }

        private void TestFunktion_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(TextPage));
        }

        private async void TestFunktion_Click2(object sender, RoutedEventArgs e)
        {
            var v = await (await Library.Current.CurrentPlaylist.CurrentSong.GetStorageFileAsync()).
                Properties.GetMusicPropertiesAsync();
            string text = "";

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
