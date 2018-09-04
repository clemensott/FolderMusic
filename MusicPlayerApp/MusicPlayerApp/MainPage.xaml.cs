using LibraryLib;
using FolderMusicLib;
using PlayerIcons;
using System;
using System.Threading;
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

        private ListBox lbxCurrentPlaylist, lbxPlaylists;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            Icons.Theme = RequestedTheme = (Background as SolidColorBrush).Color.B == 0 ? ElementTheme.Dark : ElementTheme.Light;

            DataContext = ViewModel.Current;
            timer = new Timer(new TimerCallback(UpdateSongPosition), new object(), Timeout.Infinite, 1000);

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Window.Current.Activated += Current_Activated;
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Icons.Theme = RequestedTheme;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.Current.IsMainPageLoaded)
            {
                ViewModel.Current.DoScrollLbxCurrentPlaylist();
            }

            ViewModel.Current.SetMainPageLoaded();
            //SkipSongs.AskAboutSkippedSong();
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.CodeActivated)
            {
                if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing) ChangeTimer(true);

                Library.SavePlayCommand(false);
            }
            else if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                Library.SavePlayCommand(true);
                ChangeTimer(false);
            }
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;

            if (PlaylistPage.Open) PlaylistPage.GoBack();
            else if (LoadingPage.Open) LoadingPage.GoBack();
            else Application.Current.Exit();
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                ViewModel.Current.UpdatePlayPauseIconAndText();
                ViewModel.Current.UpdateSliderValue();
                ChangeTimer(true);
            }
            else if (pauseClick && sender.CurrentState == MediaPlayerState.Paused)
            {
                ViewModel.Current.UpdatePlayPauseIconAndText();
                pauseClick = false;
            }
        }

        public void ChangeTimer(bool activate)
        {
            var timeOut = activate ? 1000 - BackgroundMediaPlayer.Current.Position.Milliseconds : Timeout.Infinite;
            timer.Change(timeOut, 1000);

            if (activate)
            {
                UpdateSongPosition(new object());
            }
        }

        private void UpdateSongPosition(object state)
        {
            ViewModel.Current.UpdateSliderValue();

            if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing)
            {
                ChangeTimer(false);
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
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
            {
                ViewModel.Current.Pause();
                pauseClick = true;
            }
            else
            {
                ViewModel.Current.Play();
            }
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

        private void CurrentPlaylistSong_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void Playlist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SetOpenPlaylist((sender as Grid).DataContext as Playlist);

            if (!PlaylistPage.Open)
            {
                Frame.Navigate(typeof(PlaylistPage));
            }
        }

        private void SetOpenPlaylist(Playlist playlist)
        {
            if (ViewModel.Current.OpenPlaylist == playlist || !ViewModel.Current.IsMainPageLoaded) return;

            ViewModel.Current.OpenPlaylist = playlist;
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
            Playlist playlist = Library.Current.CurrentPlaylist;

            Library.Current.RemoveSongFromPlaylist(playlist, songsIndex);
        }

        private async void RefreshPlaylist_Click(object sender, RoutedEventArgs e)
        {
            LoadingPage.OpenLoading(Frame);

            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;
            bool wasCurrentPlaylist = Library.Current.CurrentPlaylist == playlist;

            await playlist.LoadSongsFromStorage();

            LoadingPage.GoBack();
        }

        private async void SearchForNewSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            LoadingPage.OpenLoading(Frame);

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

        private async void RefreshEveryPlaylists_Click(object sender, RoutedEventArgs e)
        {
            LoadingPage.OpenLoading(Frame);

            await Library.Current.LoadPlaylistsFromStorage();

            LoadingPage.GoBack();
        }

        private async void SearchForNewPlaylists_Click(object sender, RoutedEventArgs e)
        {
            LoadingPage.OpenLoading(Frame);

            await Library.Current.SearchForNewPlaylists();

            LoadingPage.GoBack();
        }

        private void lbxCurrentPlaylist_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel.Current.SetLbxCurrentPlaylist(lbxCurrentPlaylist = sender as ListBox);
        }

        private async void TestFunktion_Click(object sender, RoutedEventArgs e)
        {
            await SkipSongs.AskAboutSkippedSong();
        }

        private async void TestFunktion_Click2(object sender, RoutedEventArgs e)
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync("Data.xml");

                await file.DeleteAsync();
            }
            catch { }
        }

        private void TestFunktion_Click3(object sender, RoutedEventArgs e)
        {
            double beforeMilli, deltaMilli;

            beforeMilli = DateTime.Now.TimeOfDay.TotalMilliseconds;

            deltaMilli = DateTime.Now.TimeOfDay.TotalMilliseconds - beforeMilli;
            System.Diagnostics.Debug.WriteLine("deltaMilli: " + deltaMilli.ToString());
        }

        private async void TestFunktion_Click4(object sender, RoutedEventArgs e)
        {
            try
            {
                StorageFile file1 = await ApplicationData.Current.LocalFolder.GetFileAsync("Text.txt");
                StorageFile file2 = await ApplicationData.Current.LocalFolder.GetFileAsync("Text2.txt");

                string text = await PathIO.ReadTextAsync(file1.Path);
                text += "\n" + await PathIO.ReadTextAsync(file2.Path);

                Windows.UI.Popups.IUICommand command = await new Windows.UI.Popups.MessageDialog(text).ShowAsync();
            }
            catch { }
        }
    }
}
