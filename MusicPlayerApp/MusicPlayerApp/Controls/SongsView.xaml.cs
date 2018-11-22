using MusicPlayer.Data;
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
    public abstract partial class SongsView : UserControl
    {
        enum ScrollToType { No, Last, Current }

        public static readonly DependencyProperty CurrentSongProperty =
            DependencyProperty.Register("CurrentSong", typeof(Song), typeof(SongsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnCurrentSongPropertyChanged)));

        private static void OnCurrentSongPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (SongsView)sender;
            var value = (Song)e.NewValue;

            s.SetSelectedItem();
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ISongCollection), typeof(SongsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnSourcePropertyChanged)));

        private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = sender as SongsView;
            var oldSongs = e.OldValue as ISongCollection;
            var newSongs = e.NewValue as ISongCollection;

            s.SetItemsSource();
            s.scrollTo = ScrollToType.Last;
        }

        private ScrollToType scrollTo;

        public event EventHandler<SelectedSongChangedManuallyEventArgs> SelectedSongChangedManually;

        public Song CurrentSong
        {
            get { return (Song)GetValue(CurrentSongProperty); }
            set { SetValue(CurrentSongProperty, value); }
        }

        public ISongCollection Source
        {
            get { return (ISongCollection)GetValue(SourceProperty); }
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

        protected abstract IUpdateSelectedItemCollection<Song> GetItemsSource(ISongCollection songs);

        private void ItemsSource_UpdateFinished(object sender, EventArgs e)
        {
            SetSelectedItemSafe();
        }

        private void SetSelectedItemSafe()
        {
            Utils.DoSafe(SetSelectedItem);
        }

        private void SetSelectedItem()
        {
            lbxSongs.SelectedItem = CurrentSong;
        }

        private void lbxSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Source == null || Source.Count == 0) return;

            Song selectedSong = lbxSongs.SelectedItem as Song;

            if (selectedSong != null && CurrentSong != selectedSong)
            {
                var args = new SelectedSongChangedManuallyEventArgs(CurrentSong, selectedSong);
                CurrentSong = selectedSong;
                SelectedSongChangedManually?.Invoke(this, args);
            }
            else if (lbxSongs.Items.Contains(CurrentSong))
            {
                lbxSongs.SelectedItem = CurrentSong;
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
                lbxSongs.ScrollIntoView(CurrentSong);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("ScrollToCurrentDirectFail", e);
            }
        }

        private void lbxSongs_LayoutUpdated(object sender, object e)
        {
            if (scrollTo == ScrollToType.No || lbxSongs.Items.Count == 0) return;

            ISongCollection songs = Source;

            if (songs == null)
            {
                scrollTo = ScrollToType.No;
                return;
            }

            if (lbxSongs.Items.Count < songs.Shuffle.Count) return;

            if (scrollTo == ScrollToType.Current)
            {
                lbxSongs.ScrollIntoView(CurrentSong);
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

            Source.Remove(song);
        }

        private async void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as MenuFlyoutItem).DataContext as Song;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(song.Path);

                await file.DeleteAsync();
                Source.Remove(song);
            }
            catch (FileNotFoundException)
            {
                Source.Remove(song);
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
