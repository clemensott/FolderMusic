using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;
using System.Linq;
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

            if (oldPlaylist != null) s.Unsubscibe(oldPlaylist);
            if (newPlaylist != null) s.Subscibe(newPlaylist);

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
            IUpdateSellectedItemCollection<Song> collection = GetItemsSource(Source);
            collection.UpdateFinished += ItemsSource_UpdateFinished;

            lbxSongs.ItemsSource = collection;
            SetSelectedItem();
        }

        protected abstract IUpdateSellectedItemCollection<Song> GetItemsSource(IPlaylist playlist);

        private void ItemsSource_UpdateFinished(IUpdateSellectedItemCollection<Song> sender)
        {
            SetSelectedItemSafe();
        }

        private void Subscibe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongPropertyChanged;
            playlist.ShuffleChanged += OnShuffleChanged;
            playlist.ShuffleSongs.Changed += OnShuffleSongsChanged;
            playlist.Songs.Changed += OnSongsCollectionChanged;
        }

        private void Unsubscibe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongPropertyChanged;
            playlist.ShuffleChanged -= OnShuffleChanged;
            playlist.ShuffleSongs.Changed -= OnShuffleSongsChanged;
            playlist.Songs.Changed -= OnSongsCollectionChanged;
        }

        private void OnCurrentSongPropertyChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            SetSelectedItemSafe();
        }

        private void OnShuffleChanged(IPlaylist sender, ShuffleChangedEventArgs args)
        {
            SetSelectedItemSafe();
            ScrollToCurrentSongDirect();
        }

        private void OnSongsCollectionChanged(ISongCollection sender, SongCollectionChangedEventArgs args)
        {
            SetSelectedItemSafe();
        }

        private void SetSelectedItemSafe()
        {
            MainPage.DoSafe(SetSelectedItem);
        }

        private void SetSelectedItem()
        {
            lbxSongs.SelectedItem = Source?.CurrentSong;
        }

        private void OnShuffleSongsChanged(IShuffleCollection sender)
        {
            ScrollToCurrentSongTop();
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
                MobileDebug.Manager.WriteEvent("ScrollToCurrentTopFail", e);
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
                MobileDebug.Manager.WriteEvent("ScrollToCurrentDirectFail", e);
            }
        }

        private void lbxSongs_LayoutUpdated(object sender, object e)
        {
            if (scrollTo == ScrollToType.No) return;

            IPlaylist playlist = Source;

            if (playlist == null || lbxSongs.Items.Count < playlist.ShuffleSongs.Count) return;
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
            if (((sender as Grid).DataContext as Song).IsEmpty) return;

            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void RefreshSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            song.Refresh();
        }

        private void RemoveSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            Source.Songs.Remove(song);
        }

        private void EditSong_Click(object sender, RoutedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            Song song = (sender as FrameworkElement)?.DataContext as Song;

            frame?.Navigate(typeof(SongPage), song);
        }
    }
}
