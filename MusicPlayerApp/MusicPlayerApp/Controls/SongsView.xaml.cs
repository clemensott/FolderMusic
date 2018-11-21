using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace FolderMusic
{
    public delegate void SelectedSongChangedManuallyEventHandler(object sender, SelectedSongChangedManuallyEventArgs e);

    public abstract partial class SongsView : UserControl
    {
        enum ScrollToType { No, Last, Current }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(IPlaylist), typeof(SongsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnSourcePropertyChanged)));

        private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = sender as SongsView;
            var oldPlaylist = e.OldValue as IPlaylist;
            var newPlaylist = e.NewValue as IPlaylist;

            if (oldPlaylist != null) s.Unsubscribe(oldPlaylist);
            if (newPlaylist != null) s.Subscribe(newPlaylist);

            s.SetItemsSource();
            s.scrollTo = ScrollToType.Last;
        }

        private ScrollToType scrollTo;

        public event SelectedSongChangedManuallyEventHandler SelectedSongChangedManually;

        public IPlaylist Source
        {
            get { return (IPlaylist)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public SongsView()
        {
            this.InitializeComponent();
        }

        private void control_Loaded(object sender, RoutedEventArgs e)
        {
            scrollTo = ScrollToType.Last;
        }

        private void SetItemsSource()
        {
            IUpdateSelectedItemCollection<Song> collection = GetItemsSource(Source);
            collection.UpdateFinished += ItemsSource_UpdateFinished;

            lbxSongs.ItemsSource = collection;
            SetSelectedItem();
        }

        protected abstract IUpdateSelectedItemCollection<Song> GetItemsSource(IPlaylist playlist);

        private void ItemsSource_UpdateFinished(object sender, EventArgs e)
        {
            SetSelectedItemSafe();
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongPropertyChanged;
            playlist.SongsChanged += Playlist_SongsChanged;

            Subscribe(playlist.Songs);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongPropertyChanged;
            playlist.SongsChanged -= Playlist_SongsChanged;

            Unsubscribe(playlist.Songs);
        }

        private void Subscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.ShuffleChanged += Songs_ShuffleChanged;

            Subscribe(songs.Shuffle);
        }

        private void Unsubscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.ShuffleChanged -= Songs_ShuffleChanged;

            Unsubscribe(songs.Shuffle);
        }

        private void Subscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed += Shuffle_Changed;
        }

        private void Unsubscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed -= Shuffle_Changed;
        }

        private void Playlist_SongsChanged(object sender, SongsChangedEventArgs e)
        {
            Unsubscribe(e.OldSongs);
            Subscribe(e.NewSongs);

            ScrollToCurrentSongTop();
        }

        private void Songs_ShuffleChanged(object sender, ShuffleChangedEventArgs e)
        {
            Unsubscribe(e.OldShuffleSongs);
            Subscribe(e.NewShuffleSongs);

            ScrollToCurrentSongTop();
        }

        private void Shuffle_Changed(object sender, ShuffleCollectionChangedEventArgs e)
        {
            ScrollToCurrentSongTop();
        }

        private void OnCurrentSongPropertyChanged(object sender, CurrentSongChangedEventArgs args)
        {
            SetSelectedItemSafe();
        }

        private void SetSelectedItemSafe()
        {
            Utils.DoSafe(SetSelectedItem);
        }

        private void SetSelectedItem()
        {
            lbxSongs.SelectedItem = Source?.CurrentSong;
        }

        private void lbxSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((Source?.Songs.Count ?? 0) == 0) return;

            Song selectedSong = lbxSongs.SelectedItem as Song;

            if (selectedSong != null && Source.CurrentSong != selectedSong)
            {
                var args = new SelectedSongChangedManuallyEventArgs(Source.CurrentSong, selectedSong);
                Source.CurrentSong = selectedSong;
                SelectedSongChangedManually?.Invoke(this, args);
            }
            else if (lbxSongs.Items.Contains(Source.CurrentSong))
            {
                lbxSongs.SelectedItem = Source.CurrentSong;
            }
        }

        public void ScrollToCurrentSongTop()
        {
            try
            {
                scrollTo = ScrollToType.Last;
                lbxSongs.ScrollIntoView(lbxSongs.Items.LastOrDefault());
                scrollTo = ScrollToType.Current;
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("ScrollToCurrentTopFail", e);
            }
        }

        public void ScrollToCurrentSongDirect()
        {
            try
            {
                lbxSongs.ScrollIntoView(Source?.CurrentSong);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("ScrollToCurrentDirectFail", e);
            }
        }

        private void lbxSongs_LayoutUpdated(object sender, object e)
        {
            if (scrollTo == ScrollToType.No) return;

            IPlaylist playlist = Source;

            if (playlist == null || lbxSongs.Items.Count < playlist.Songs.Shuffle.Count || lbxSongs.Items.Count == 0) return;
            if (scrollTo == ScrollToType.Current)
            {
                lbxSongs.ScrollIntoView(playlist.CurrentSong);
                scrollTo = ScrollToType.No;
            }
            else
            {
                lbxSongs.ScrollIntoView(lbxSongs.Items.Last());
                scrollTo = ScrollToType.Current;
            }
        }

        private void Song_Holding(object sender, HoldingRoutedEventArgs e)
        {
            MobileDebug.Service.WriteEvent("Song_Holding", ActualWidth);
            if (((sender as Grid).DataContext as Song).IsEmpty) return;

            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void ResetSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            await song.Reset();
        }

        private void RemoveSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            Source.Songs.Remove(song);
        }

        private async void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(song.Path);

                await file.DeleteAsync();
                Source.Songs.Remove(song);
            }
            catch (FileNotFoundException)
            {
                Source.Songs.Remove(song);
            }
            catch (Exception exc)
            {
                await new MessageDialog(exc.Message, exc.GetType().Name).ShowAsync();
            }
        }

        private void EditSong_Click(object sender, RoutedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            Song song = (sender as FrameworkElement)?.DataContext as Song;

            frame?.Navigate(typeof(SongPage), song);
        }
    }
}
