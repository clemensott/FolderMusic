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
using FolderMusic.EventArgs;
using MusicPlayer.Models;
using MusicPlayer.Models.EventArgs;
using FolderMusic.NavigationParameter;
using MusicPlayer.Models.Foreground.Interfaces;
using MusicPlayer.Models.Foreground.Shuffle;
using MusicPlayer.UpdateLibrary;
using Windows.UI.Xaml.Media;
using System.Threading.Tasks;

namespace FolderMusic
{
    public partial class SongsView : UserControl
    {
        public static readonly DependencyProperty CurrentSongProperty = DependencyProperty.Register("CurrentSong",
            typeof(Song?), typeof(SongsView), new PropertyMetadata(null, OnCurrentSongPropertyChanged));

        private static void OnCurrentSongPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SongsView s = (SongsView)sender;

            s.SetSelectedItem();
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ISongCollection), typeof(SongsView),
                new PropertyMetadata(null, OnSourcePropertyChanged));

        private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SongsView s = (SongsView)sender;
            ISongCollection oldSongs = e.OldValue as ISongCollection;
            ISongCollection newSongs = e.NewValue as ISongCollection;

            s.OnSourceChanged(oldSongs, newSongs);
        }

        private bool needScrollToCurrentSong;
        private ScrollViewer scrollViewer;

        public event EventHandler<SelectedSongChangedManuallyEventArgs> SelectedSongChangedManually;

        public Song? CurrentSong
        {
            get { return (Song?)GetValue(CurrentSongProperty); }
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

        protected virtual void OnSourceChanged(ISongCollection oldSongs, ISongCollection newSongs)
        {
            Unsubscribe(oldSongs);
            Subscribe(newSongs);

            SetItemsSource(Source.Shuffle.ToArray());
            ScrollToCurrentSong();
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
            ScrollToCurrentSong();
        }

        private void Shuffle_Changed(object sender, ShuffleCollectionChangedEventArgs e)
        {
            SetItemsSource(Source.Shuffle.ToArray());
        }

        protected void SetItemsSource(IEnumerable<Song> songs)
        {
            lbxSongs.ItemsSource = songs as IList<Song> ?? songs?.ToArray();
            SetSelectedItem();
        }

        private void SetSelectedItem()
        {
            lbxSongs.SelectedItem = CurrentSong;
            CheckNeedsScrolling();
        }

        private void LbxSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Source == null || Source.Count == 0) return;

            Song? selectedSong = lbxSongs.SelectedItem as Song?;

            if (selectedSong != null && !Equals(CurrentSong, selectedSong))
            {
                SelectedSongChangedManuallyEventArgs args =
                    new SelectedSongChangedManuallyEventArgs(CurrentSong, selectedSong);

                CurrentSong = selectedSong;
                SelectedSongChangedManually?.Invoke(this, args);
            }
            else if (!Equals(selectedSong, CurrentSong) && lbxSongs.Items.Contains(CurrentSong))
            {
                lbxSongs.SelectedItem = CurrentSong;
            }
        }

        private void LbxSongs_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CheckNeedsScrolling();
        }

        private void GidItemContainer_Loaded(object sender, RoutedEventArgs e)
        {
            CheckNeedsScrolling();
        }

        private void CheckNeedsScrolling()
        {
            if (needScrollToCurrentSong) ScrollToCurrentSong();
        }

        public async void ScrollToCurrentSong()
        {
            try
            {
                needScrollToCurrentSong = true;

                if (scrollViewer == null && !TryFindScrollView((DependencyObject)lbxSongs, out scrollViewer)) return;

                int index = lbxSongs.Items.IndexOf(CurrentSong);
                if (index == -1) return;

                double scrollOffset = index;
                if (scrollOffset > scrollViewer.ScrollableHeight) scrollOffset = scrollViewer.ScrollableHeight;
                else if (scrollOffset < 0) scrollOffset = 0;

                scrollViewer.ChangeView(null, scrollOffset, null);

                await Task.Delay(100);
                needScrollToCurrentSong = false;
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("ScrollToCurrentDirectFail", e);
            }
        }

        private static bool TryFindScrollView(DependencyObject db, out ScrollViewer sv)
        {
            if (db is ScrollViewer)
            {
                sv = (ScrollViewer)db;
                return true;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(db); i++)
            {
                if (TryFindScrollView(VisualTreeHelper.GetChild(db, i), out sv)) return true;
            }

            sv = null;
            return false;
        }

        private void Song_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (!((Song?)((FrameworkElement)sender).DataContext).HasValue) return;

            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void ResetSong_Click(object sender, RoutedEventArgs e)
        {
            Song oldSong = (Song)((MenuFlyoutItem)sender).DataContext;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(oldSong.FullPath);
                Song? newSong = await UpdateLibraryUtils.LoadSong(file);
                if (newSong.HasValue)
                {
                    if (!Equals(newSong.Value, oldSong))
                    {
                        Source.Change(new Song[] { oldSong }, new Song[] { newSong.Value });
                    }
                }
                else await new MessageDialog("Reloading song failed").ShowAsync();
            }
            catch (Exception exc)
            {
                await new MessageDialog(exc.Message, exc.GetType().Name).ShowAsync();
            }
        }

        private async void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((MenuFlyoutItem)sender).DataContext;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(song.FullPath);

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
            Song? song = (sender as FrameworkElement)?.DataContext as Song?;

            if (!song.HasValue) return;

            SongPageParameter parameter = new SongPageParameter(song.Value, Source);
            frame?.Navigate(typeof(SongPage), parameter);
        }
    }
}
