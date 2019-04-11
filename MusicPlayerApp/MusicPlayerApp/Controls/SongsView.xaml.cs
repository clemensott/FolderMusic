using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
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
    public partial class SongsView : UserControl
    {
        enum ScrollToType { No, Last, Current }

        public static readonly DependencyProperty CurrentSongProperty =
            DependencyProperty.Register("CurrentSong", typeof(Song), typeof(SongsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnCurrentSongPropertyChanged)));

        private static void OnCurrentSongPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SongsView s = (SongsView)sender;

            MobileDebug.Service.WriteEvent(s.GetType() + ".OnCurrentSongChanged", e.OldValue, e.NewValue);
            s.SetSelectedItem();
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ISongCollection), typeof(SongsView),
                new PropertyMetadata(null, new PropertyChangedCallback(OnSourcePropertyChanged)));

        private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SongsView s = (SongsView)sender;
            ISongCollection oldSongs = e.OldValue as ISongCollection;
            ISongCollection newSongs = e.NewValue as ISongCollection;

            s.OnSourceChanged(oldSongs, newSongs);
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

        protected virtual void OnSourceChanged(ISongCollection oldSongs, ISongCollection newSongs)
        {
            Unsubscribe(oldSongs);
            Subscribe(newSongs);

            SetItemsSource(Source.Shuffle.ToArray());
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
            if (shuffle == null) return;

            shuffle.Changed += Shuffle_Changed;
        }

        private void Unsubscribe(IShuffleCollection shuffle)
        {
            if (shuffle == null) return;

            shuffle.Changed -= Shuffle_Changed;
        }

        private void Songs_ShuffleChanged(object sender, ShuffleChangedEventArgs e)
        {
            Unsubscribe(e.OldShuffleSongs);
            Subscribe(e.NewShuffleSongs);

            SetItemsSource(Source.Shuffle.ToArray());
        }

        private void Shuffle_Changed(object sender, ShuffleCollectionChangedEventArgs e)
        {
            SetItemsSource(Source.Shuffle.ToArray());
        }

        protected void SetItemsSource(IEnumerable<Song> songs)
        {
            lbxSongs.ItemsSource = songs?.ToArray();
            SetSelectedItem();
            ScrollToCurrentSongDirect();
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
                SelectedSongChangedManuallyEventArgs args =
                    new SelectedSongChangedManuallyEventArgs(CurrentSong, selectedSong);

                CurrentSong = selectedSong;
                SelectedSongChangedManually?.Invoke(this, args);
            }
            else if (selectedSong != CurrentSong && lbxSongs.Items.Contains(CurrentSong))
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
            if (((Song)((FrameworkElement)sender).DataContext).IsEmpty) return;

            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void ResetSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((MenuFlyoutItem)sender).DataContext;

            await song.Reset();
        }

        private void RemoveSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((MenuFlyoutItem)sender).DataContext;

            Source.Remove(song);
        }

        private async void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((MenuFlyoutItem)sender).DataContext;

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
