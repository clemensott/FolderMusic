using PlayerIcons;
using PlaylistSong;
using System;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
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

            if (Library.Current.CurrentPlaylist.Shuffle == ShuffleKind.Complete)
            {
                BackgroundCommunicator.SendGetCurrent();
            }

            DataContext = App.ViewModel;
            timer = new Timer(new TimerCallback(UpadateSongPostion), new object(), Timeout.Infinite, 1000);

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Library.Current.Loaded += Library_Loaded;
            Window.Current.Activated += Current_Activated;
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Icons.Theme = RequestedTheme;
            rtLoading.Fill = Background;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.MainPageLoaded)
            {
                BackgroundCommunicator.SendRun();

                LbxCurrentPlaylistScollToSelectedItem();
                LbxPlaylistsScollToSelectedItem();
            }

            App.ViewModel.SetMainPageLoaded();
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.CodeActivated)
            {
                if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
                {
                    ChangeTimer(true);
                }

                UiUpdate.AfterActivating();
                Library.SavePlayCommand(false);
            }
            else if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                ChangeTimer(false);
                Library.SavePlayCommand(true);
            }
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (PlaylistPage.Open)
            {
                e.Handled = true;
                PlaylistPage.GoBack();
            }
            else
            {
                Application.Current.Exit();
            }
        }

        private async void Library_Loaded()
        {
            BackgroundCommunicator.SendLoad();

            UiUpdate.All();

            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundCommunicator.SetReceivedEvent();

            StopLoading();

            await CoreApplication.MainView.CoreWindow.Dispatcher.
                RunAsync(CoreDispatcherPriority.Normal, async () =>
                { await new MessageDialog("Loading Complete").ShowAsync(); });
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                UiUpdate.PlayPauseIcon();
                UiUpdate.CurrentSongNaturalDuration();
                ChangeTimer(true);
            }
            else if (pauseClick && sender.CurrentState == MediaPlayerState.Paused)
            {
                UiUpdate.PlayPauseIcon();
            }
        }

        public void ChangeTimer(bool activate)
        {
            var timeOut = activate ? 1000 - BackgroundMediaPlayer.Current.Position.Milliseconds : Timeout.Infinite;
            timer.Change(timeOut, 1000);

            if (activate)
            {
                UpadateSongPostion(new object());
            }
        }

        private void UpadateSongPostion(object state)
        {
            UiUpdate.CurrentSongPosition();

            if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing)
            {
                ChangeTimer(false);
            }
        }

        private void Shuffle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                Library.Current.CurrentPlaylist.SetNextShuffle();
                BackgroundCommunicator.SendShuffle(Library.Current.CurrentPlaylistIndex);
                UiUpdate.CurrentPlaylistSongsAndIndex();
                UiUpdate.CurrentSongIndex();
                Library.SaveAsync();
            }
            catch { }

            UiUpdate.ShuffleIcon();
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
            BackgroundCommunicator.SendPrevious();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
            {
                BackgroundCommunicator.SendPause();
                pauseClick = true;
            }
            else
            {
                BackgroundCommunicator.SendPlay();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            BackgroundCommunicator.SendNext();
        }

        private void Loop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                Library.Current.CurrentPlaylist.SetNextLoop();

                BackgroundCommunicator.SendLoop(Library.Current.CurrentPlaylistIndex);
            }
            catch { }

            UiUpdate.LoopIcon();
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
            SetCurrentSong((sender as Grid).DataContext as Song);
        }

        private void SetCurrentSong(Song song)
        {
            if (Library.Current.CurrentPlaylist.CurrentSong == song || song.IsEmpty || !App.ViewModel.MainPageLoaded) return;

            int index = Library.Current.CurrentPlaylist.GetShuffleListIndex(song);

            if (Library.Current.CurrentPlaylist.CurrentSongIndex == index) return;

            BackgroundCommunicator.SendPlaySong(index);
            Library.Current.CurrentPlaylist.CurrentSongIndex = index;
            UiUpdate.CurrentSongTitleArtistNaturalDuration();
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
            if (App.ViewModel.OpenPlaylist == playlist || !App.ViewModel.MainPageLoaded) return;

            App.ViewModel.OpenPlaylist = playlist;
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            App.ViewModel.CurrentPlaylist = (sender as Image).DataContext as Playlist;

            BackgroundCommunicator.SendCurrentPlaylistIndex(true);
            LbxCurrentPlaylistScollToSelectedItem();

            Library.SaveAsync();
        }

        private void PlaylistsPlaylist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void CurrentPlaylistSong_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;
            Playlist currentPlaylist = Library.Current.CurrentPlaylist;

            Library.Current.RemoveSongFromCurrentPlaylist(song);
            BackgroundCommunicator.SendRemoveSong(Library.Current.CurrentPlaylist.GetShuffleListIndex(song));

            if (currentPlaylist.IsEmpty())
            {
                UiUpdate.AfterDeleteCurrentPlaylist();
                LbxCurrentPlaylistScollToSelectedItem();
            }
            else UiUpdate.CurrentPlaylistSongsAndIndex();

            Library.SaveAsync();
        }

        private void sld_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            App.ViewModel.EnteredSlider();
        }

        private void Page_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            App.ViewModel.SetSliderValue();
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;
            bool isCurrent = Library.Current.CurrentPlaylist == playlist;

            BackgroundCommunicator.SendRemovePlaylist(Library.Current.GetPlaylists().IndexOf(playlist));
            Library.Current.Delete(playlist);

            if (isCurrent)
            {
                UiUpdate.AfterDeleteCurrentPlaylist();
                LbxCurrentPlaylistScollToSelectedItem();
            }
            else UiUpdate.PlaylistsAndCurrentPlaylistIndex();

            Library.SaveAsync();
        }

        private async void RefreshPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;
            int songsCount = playlist.Lenght;
            int playlistsCount = Library.Current.Lenght;

            StartLoading();
            await playlist.LoadSongsFromStorage();

            if (Library.Current.Lenght != playlistsCount) UiUpdate.PlaylistsAndCurrentPlaylistIndex();
            if (Library.Current.CurrentPlaylist == playlist) UiUpdate.CurrentPlaylistIndexAndRest();
            if (playlist.Lenght != songsCount) UiUpdate.PlaylistsAndCurrentPlaylistIndex();

            StopLoading();
            Library.Save();
            BackgroundCommunicator.SendLoad();
        }

        private void RefreshEveryPlaylists_Click(object sender, RoutedEventArgs e)
        {
            StartLoading();
            Library.DeleteSongIndexAndMilliseconds();
            Library.Current.LoadPlaylistsFromStorage();
            //BackgroundCommunicator.SendPause();
        }

        private void SearchForNewPlaylists_Click(object sender, RoutedEventArgs e)
        {
            StartLoading();
            Library.Current.SearchForNewPlaylists();
        }

        private void StartLoading()
        {
            App.ViewModel.IsUiEnabled = false;
            pbLoading.IsEnabled = true;
            gdLoading.Visibility = Visibility.Visible;
        }

        private void lbxCurrentPlaylist_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            lbxCurrentPlaylist = sender as ListBox;
        }

        private void LbxCurrentPlaylistScollToSelectedItem()
        {
            if (lbxCurrentPlaylist == null || lbxCurrentPlaylist.SelectedIndex == -1) return;
            System.Diagnostics.Debug.WriteLine(lbxCurrentPlaylist.SelectedItem);
            lbxCurrentPlaylist.ScrollIntoView(lbxCurrentPlaylist.SelectedItem);
        }

        private void lbxPlaylists_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            lbxPlaylists = sender as ListBox;
        }

        private void LbxPlaylistsScollToSelectedItem()
        {
            if (lbxPlaylists == null || lbxPlaylists.SelectedIndex == -1) return;

            lbxPlaylists.ScrollIntoView(lbxPlaylists.SelectedItem);
        }

        private void StopLoading()
        {
            App.ViewModel.IsUiEnabled = true;
            pbLoading.IsEnabled = false;
            gdLoading.Visibility = Visibility.Collapsed;
        }

        private void TestFunktion_Click(object sender, RoutedEventArgs e)
        {
            UiUpdate.CurrentPlaylistIndex();
            UiUpdate.CurrentSongIndex();
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
    }
}
