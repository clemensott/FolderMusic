using PlayerIcons;
using PlaylistSong;
using System;
using System.Collections.Generic;
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
        private bool playPlaylistTappedOrHolding = false, pauseClick = false, 
            loopImageEntered = false, shuffleImageEntered = false;

        private Timer timer;

        private ListBox lbxCurrentPlaylist, lbxPlaylists;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            Icons.Theme = RequestedTheme = (Background as SolidColorBrush).Color.B == 0 ?
                ElementTheme.Dark : ElementTheme.Light;

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
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.MainPageLoaded)
            {
                lbx_SelectionChanged(lbxCurrentPlaylist, new SelectionChangedEventArgs(new List<object>(), new List<object>()));
                lbx_SelectionChanged(lbxPlaylists, new SelectionChangedEventArgs(new List<object>(), new List<object>()));
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

                App.ViewModel.UpdateAfterActivating();
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

                App.ViewModel.CurrentPlaylistIndex = App.ViewModel.CurrentPlaylistIndex;
            }
            else
            {
                Application.Current.Exit();
            }
        }

        private async void Library_Loaded()
        {
            BackgroundCommunicator.SendLoad();

            App.ViewModel.UpdateAll();

            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundCommunicator.SetReceivedEvent();

            LoadingBar.IsEnabled = false;
            LoadingBar.Visibility = Visibility.Collapsed;

            await CoreApplication.MainView.CoreWindow.Dispatcher.
                RunAsync(CoreDispatcherPriority.Normal, async () =>
                { await new MessageDialog("Loading Complete").ShowAsync(); });
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                App.ViewModel.UpdatePlayPauseIcon();
                App.ViewModel.UpdateCurrentSongNaturalDuration();
                ChangeTimer(true);
            }
            else if (pauseClick && sender.CurrentState == MediaPlayerState.Paused)
            {
                App.ViewModel.UpdatePlayPauseIcon();
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
            App.ViewModel.UpdateCurrentSongPosition();

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
                App.ViewModel.UpdateCurrentPlaylistSongsAndIndex();
                App.ViewModel.UpdateCurrentSongIndex();
                Library.SaveAsync();
            }
            catch { }

            App.ViewModel.UpdateShuffleIcon();
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

            App.ViewModel.UpdateLoopIcon();
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

        private void lbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lbx = sender as ListBox;

            if (lbx == null) return;

            if (lbxCurrentPlaylist == null && lbx.Name == "lbxCurrentPlaylist") lbxCurrentPlaylist = lbx;
            if (lbxPlaylists == null && lbx.Name == "lbxPlaylists") lbxPlaylists = lbx;

            if (App.ViewModel.MainPageLoaded && lbx.SelectedIndex != -1)
            {
                lbx.ScrollIntoView(lbx.SelectedItem);
            }
        }

        private void lbxCurrentPlaylist_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            App.ViewModel.LbxCurrentPlaylistEntered();
        }

        private void Playlist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (playPlaylistTappedOrHolding)
            {
                playPlaylistTappedOrHolding = false;
                return;
            }

            playPlaylistTappedOrHolding = false;
            App.ViewModel.SetLbxPlaylistsChangedIndex(Library.Current.GetPlaylists().IndexOf((sender as Grid).DataContext as Playlist));

            if (!PlaylistPage.Open)
            {
                Frame.Navigate(typeof(PlaylistPage));
            }
        }

        private void PlayPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            playPlaylistTappedOrHolding = true;

            App.ViewModel.SetLbxPlaylistsChangedIndex(Library.Current.GetPlaylists().IndexOf((sender as Image).DataContext as Playlist));
            App.ViewModel.SetCurrentPlaylistIndex();

            BackgroundCommunicator.SendCurrentPlaylistIndex(true);
            Library.SaveAsync();
        }

        private void PlayImage_Holding(object sender, HoldingRoutedEventArgs e)
        {
            playPlaylistTappedOrHolding = true;
        }

        private void PlaylistsPlaylist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (playPlaylistTappedOrHolding)
            {
                playPlaylistTappedOrHolding = false;
                return;
            }

            playPlaylistTappedOrHolding = false;
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void CurrentPlaylistSong_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            string currentPlaylistAbsolutePath = Library.Current.CurrentPlaylist.AbsolutePath;
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            Library.Current.RemoveSongFromCurrentPlaylist(song);
            BackgroundCommunicator.SendRemoveSong(Library.Current.CurrentPlaylist.GetShuffleSongs().IndexOf(song));

            if (currentPlaylistAbsolutePath == Library.Current.CurrentPlaylist.AbsolutePath)
            {
                App.ViewModel.UpdateCurrentPlaylistSongsAndIndex();
                App.ViewModel.UpdateCurrentSongIndex();
            }
            else
            {
                App.ViewModel.UpdateCurrentPlaylistsIndexAndRest();
            }

            Library.SaveAsync();
        }

        private void sld_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            App.ViewModel.EnteredSlider();
        }

        private void Page_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            App.ViewModel.SetSliderValue();
            App.ViewModel.LbxCurrentPlaylistExited();
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;
            BackgroundCommunicator.SendRemovePlaylist(Library.Current.GetPlaylists().IndexOf(playlist));

            if (Library.Current.CurrentPlaylist == playlist)
            {
                Library.Current.Delete(playlist);
                App.ViewModel.UpdateCurrentPlaylistsIndexAndRest();
            }
            else
            {
                Library.Current.Delete(playlist);
                App.ViewModel.UpdatePlaylistsAndCurrentPlaylistIndex();
            }

            Library.SaveAsync();
        }

        private async void RefreshPlaylist_Click(object sender, RoutedEventArgs e)
        {
            int count = Library.Current.Lenght;
            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;
            await playlist.LoadSongsFromStorage();

            if (Library.Current.Lenght != count) App.ViewModel.UpdatePlaylistsAndCurrentPlaylistIndex();
            if (Library.Current.CurrentPlaylist == playlist) App.ViewModel.UpdateCurrentPlaylistsIndexAndRest();

            Library.Save();
            BackgroundCommunicator.SendLoad();
        }

        private void RefreshEveryPlaylists_Click(object sender, RoutedEventArgs e)
        {
            LoadingBar.IsEnabled = true;
            LoadingBar.Visibility = Visibility.Visible;

            Library.DeleteSongIndexAndMilliseconds();
            Library.Current.LoadPlaylistsFromStorage();
            //BackgroundCommunicator.SendPause();
        }

        private void SearchForNewPlaylists_Click(object sender, RoutedEventArgs e)
        {
            LoadingBar.IsEnabled = true;
            LoadingBar.Visibility = Visibility.Visible;

            Library.Current.SearchForNewPlaylists();
        }

        private void TestFunktion_Click(object sender, RoutedEventArgs e)
        {/*
            StorageFile fileTime = await ApplicationData.Current.LocalFolder.GetFileAsync("Time.txt");
            StorageFile fileTask = await ApplicationData.Current.LocalFolder.GetFileAsync("Task.txt");
            StorageFile fileCancel = await ApplicationData.Current.LocalFolder.GetFileAsync("Cancel.txt");
            StorageFile fileTimer = await ApplicationData.Current.LocalFolder.GetFileAsync("Timer.txt");

            string text = await PathIO.ReadTextAsync(fileTime.Path) + "\n";
            text += await PathIO.ReadTextAsync(fileTask.Path) + "\n";
            text += await PathIO.ReadTextAsync(fileCancel.Path) + "\n";
            text += await PathIO.ReadTextAsync(fileTimer.Path);

            await new MessageDialog(text).ShowAsync();      //          */

            LoadingBar.IsEnabled = !LoadingBar.IsEnabled;
            LoadingBar.Visibility = LoadingBar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
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
