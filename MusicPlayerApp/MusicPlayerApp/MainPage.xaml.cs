using LibraryLib;
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

            BackgroundCommunicator.SendGetXmlText();
            Icons.Theme = RequestedTheme = (Background as SolidColorBrush).Color.B == 0 ? ElementTheme.Dark : ElementTheme.Light;

            DataContext = App.ViewModel;
            timer = new Timer(new TimerCallback(UpadateSongPostion), new object(), Timeout.Infinite, 1000);

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
            if (!App.ViewModel.MainPageLoaded)
            {
                App.ViewModel.LbxCurrentPlaylistScollToSelectedItem();
                App.ViewModel.LbxPlaylistsScollToSelectedItem();
            }

            App.ViewModel.SetMainPageLoaded();
            SkipSongs.AskAboutSkippedSong();
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
            e.Handled = true;

            if (PlaylistPage.Open) PlaylistPage.GoBack();
            else if (LoadingPage.Open) LoadingPage.GoBack();
            else Application.Current.Exit();
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
            App.ViewModel.SetChangedCurrentPlaylistIndex();
            BackgroundCommunicator.SendShuffle(Library.Current.CurrentPlaylistIndex);

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
            Library.Current.CurrentPlaylist.SetNextLoop();

            BackgroundCommunicator.SendLoop(Library.Current.CurrentPlaylistIndex);

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
            if (Library.Current.CurrentPlaylist.CurrentSong == song ||
                song.IsEmptyOrLoading || !App.ViewModel.MainPageLoaded) return;

            int songsIndex = Library.Current.CurrentPlaylist.Songs.IndexOf(song);

            if (Library.Current.CurrentPlaylist.SongsIndex == songsIndex) return;

            BackgroundCommunicator.SendPlaySong(Library.Current.CurrentPlaylistIndex, songsIndex);
            Library.Current.CurrentPlaylist.SongsIndex = songsIndex;
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
            Playlist playlist = (sender as Image).DataContext as Playlist;

            if (playlist.IsEmptyOrLoading) return;

            App.ViewModel.CurrentPlaylist = playlist;
            App.ViewModel.SetChangedCurrentPlaylistIndex();

            BackgroundCommunicator.SendCurrentPlaylistIndex(true);
        }

        private void PlaylistsPlaylist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            //if (((sender as Grid).DataContext as Playlist).IsEmptyOrLoading) return;

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

            BackgroundCommunicator.SendSong(song);
            song.UpdateTitleAndArtist();
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;
            int songsIndex = Library.Current.CurrentPlaylist.Songs.IndexOf(song);
            Playlist playlist = Library.Current.CurrentPlaylist;

            Library.Current.RemoveSongFromPlaylist(playlist, songsIndex);
            BackgroundCommunicator.SendRemoveSong(Library.Current.CurrentPlaylistIndex, songsIndex);

            if (playlist.IsEmptyOrLoading)
            {
                UiUpdate.PlaylistsAndCurrentPlaylist();
                App.ViewModel.LbxCurrentPlaylistScollToSelectedItem();
            }
            else
            {
                UiUpdate.CurrentPlaylistSongs();
                Library.Current.CurrentPlaylist.UpdateSongCount();
            }
        }

        private async void RefreshPlaylist_Click(object sender, RoutedEventArgs e)
        {
            OpenLoadingPage();

            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;
            bool wasCurrentPlaylist = Library.Current.CurrentPlaylist == playlist;
            int songsCount = playlist.Length;
            int playlistsCount = Library.Current.Length;

            await playlist.LoadSongsFromStorage();

            if (Library.Current.Length == playlistsCount) BackgroundCommunicator.SendPlaylistXML(playlist);
            else BackgroundCommunicator.SendRemovePlaylist(playlist);

            if (Library.Current.Length != playlistsCount) UiUpdate.Playlists();
            if (playlist.Length != songsCount && !playlist.IsEmptyOrLoading) playlist.UpdateSongCount();
            if (Library.Current.CurrentPlaylist == playlist || wasCurrentPlaylist) UiUpdate.CurrentPlaylistIndexAndRest();

            CloseLoadingPage();
        }

        private async void SearchForNewSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            OpenLoadingPage();

            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;
            int songCount = playlist.Songs.Count;
            await playlist.SearchForNewSongs();


            if (playlist.Songs.Count != songCount)
            {
                BackgroundCommunicator.SendPlaylistXML(playlist);
                UiUpdate.Playlists();
                playlist.UpdateSongCount();
            }

            if (Library.Current.CurrentPlaylist == playlist) UiUpdate.CurrentPlaylistSongs();

            CloseLoadingPage();
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = (sender as MenuFlyoutItem).DataContext as Playlist;
            bool isCurrent = Library.Current.CurrentPlaylist == playlist;

            BackgroundCommunicator.SendRemovePlaylist(playlist);
            Library.Current.Delete(playlist);

            if (isCurrent)
            {
                UiUpdate.PlaylistsAndCurrentPlaylist();
                App.ViewModel.LbxCurrentPlaylistScollToSelectedItem();
            }
            else UiUpdate.Playlists();
        }

        private void sld_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            App.ViewModel.EnteredSlider();
        }

        private void Page_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            App.ViewModel.SetSliderValue();
        }

        private async void RefreshEveryPlaylists_Click(object sender, RoutedEventArgs e)
        {
            OpenLoadingPage();

            BackgroundCommunicator.SendPause();
            await Library.Current.LoadPlaylistsFromStorage();
            BackgroundCommunicator.SendLoadXML(true, Library.Current.GetXmlText());
            UiUpdate.PlaylistsAndCurrentPlaylist();

            CloseLoadingPage();
        }

        private async void SearchForNewPlaylists_Click(object sender, RoutedEventArgs e)
        {
            OpenLoadingPage();

            await Library.Current.SearchForNewPlaylists();
            BackgroundCommunicator.SendLoadXML(true, Library.Current.GetXmlText());
            UiUpdate.PlaylistsAndCurrentPlaylist();

            CloseLoadingPage();
        }

        private void OpenLoadingPage()
        {
            if (LoadingPage.Open) return;
            Frame.Navigate(typeof(LoadingPage));
        }

        private void CloseLoadingPage()
        {
            LoadingPage.GoBack();
        }

        private void lbxCurrentPlaylist_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            App.ViewModel.SetLbxCurrentPlaylist(lbxCurrentPlaylist = sender as ListBox);
        }

        private void lbxPlaylists_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            App.ViewModel.SetLbxPlaylists(lbxPlaylists = sender as ListBox);
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

        private void TestFunktion_Click4(object sender, RoutedEventArgs e)
        {

        }
    }
}
