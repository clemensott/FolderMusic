using MusicPlayer.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace FolderMusic
{
    public sealed partial class SongListView : UserControl
    {
        enum ScrollToType { No, Last, Current }

        public enum SongsSourceType { Default, Shuffle }

        public static readonly DependencyProperty SongsSourceProperty =
            DependencyProperty.Register("SongsSource", typeof(SongsSourceType), typeof(SongListView),
                new PropertyMetadata(SongsSourceType.Default, new PropertyChangedCallback(OnSongsSourcePropertyChanged)));

        private static void OnSongsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = sender as SongListView;

            if (s == null) return;

            s.viewModel.UpdateSongListAndSelectedIndex();
            s.viewModel.UpdateSelectedIndex();

            s.ScrollToCurrentSongTop();
        }

        public static readonly DependencyProperty PlaylistProperty =
            DependencyProperty.Register("Source", typeof(Playlist), typeof(SongListView),
                new PropertyMetadata(Library.Current.CurrentPlaylist, new PropertyChangedCallback(OnSourcePropertyChanged)));

        private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = sender as SongListView;

            if (s == null) return;

            s.viewModel.UpdateSongListAndSelectedIndex();

            s.ScrollToCurrentSongTop();
        }

        private SongListViewModel viewModel;
        private ScrollToType scrollTo;

        public SongsSourceType SongsSource
        {
            get { return (SongsSourceType)GetValue(SongsSourceProperty); }
            set { SetValue(SongsSourceProperty, value); }
        }

        public Playlist Source
        {
            get { return (Playlist)GetValue(PlaylistProperty); }
            set { SetValue(PlaylistProperty, value); }
        }

        public SongListView()
        {
            this.InitializeComponent();

            viewModel = new SongListViewModel(this);
            lbxSongs.DataContext = viewModel;

            scrollTo = ScrollToType.Last;

            Feedback.Current.OnLibraryChanged += OnLibraryChanged;
            Feedback.Current.OnCurrentSongPropertyChanged += OnCurrentSongPropertyChanged;
            Feedback.Current.OnSongsPropertyChanged += OnSongsPropertyChanged;
            Feedback.Current.OnShufflePropertyChanged += OnShufflePropertyChanged;
        }

        private void OnLibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            viewModel.UpdateSongListAndSelectedIndex();

            ScrollToCurrentSongTop();
        }

        private void OnCurrentSongPropertyChanged(Playlist sender, CurrentSongChangedEventArgs args)
        {
            viewModel.UpdateSelectedIndex();
        }

        private void OnSongsPropertyChanged(Playlist sender, SongsChangedEventArgs args)
        {
            if (Source != sender) return;

            viewModel.UpdateSongListAndSelectedIndex();
        }

        private void OnShufflePropertyChanged(Playlist sender, ShuffleChangedEventArgs args)
        {
            viewModel.UpdateSongListAndSelectedIndex();
        }

        public void ScrollToCurrentSongTop()
        {
            scrollTo = ScrollToType.Last;

            try
            {
                lbxSongs.ScrollIntoView(lbxSongs.Items.Last());
                scrollTo = ScrollToType.Current;
            }
            catch { }
        }

        public void ScrollToCurrentSongDirect()
        {
            try
            {
                lbxSongs.ScrollIntoView(lbxSongs.Items[Source.ShuffleListIndex]);
            }
            catch { }
        }

        private async void lbxSongs_LayoutUpdated(object sender, object e)
        {
            if (scrollTo == ScrollToType.No) return;

            Playlist playlist = Source;

            if (lbxSongs.Items.Count < playlist.ShuffleList.Count) return;

            if (scrollTo == ScrollToType.Current)
            {
                lbxSongs.ScrollIntoView(lbxSongs.Items[playlist.ShuffleListIndex]);
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
            if (((sender as Grid).DataContext as Song).IsEmptyOrLoading) return;

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

            Library.Current.CurrentPlaylist.Songs.Remove(song);
        }

        class SongListViewModel : INotifyPropertyChanged
        {
            private bool isUpdatingSongList;
            private SongListView parent;

            public int SelectedIndex
            {
                get { return GetSelectedIndex(); }
                set
                {
                    if (value == SelectedIndex || isUpdatingSongList) return;

                    SetSelctedIndex(value);
                    UpdateSelectedIndex();
                }
            }

            public IEnumerable<Song> SongList
            {
                get { return GetSongList(); }
            }

            public SongListViewModel(SongListView parent)
            {
                this.parent = parent;
            }

            private int GetSelectedIndex()
            {
                if (SongList.Count() == 0 || isUpdatingSongList) return -1;

                return parent.SongsSource == SongsSourceType.Default ?
                    parent.Source.SongsIndex : parent.Source.ShuffleListIndex;
            }

            private void SetSelctedIndex(int index)
            {
                if (parent.Source == null) return;

                if (parent.SongsSource == SongsSourceType.Default) parent.Source.SongsIndex = index;
                else parent.Source.ShuffleListIndex = index;
            }

            private Song[] GetSongList()
            {
                if (parent.Source == null) return new Song[0];

                var songs = parent.SongsSource == SongsSourceType.Default ?
                    parent.Source.Songs : parent.Source.ShuffleSongs;

                return songs.ToArray();
            }

            public void UpdateSelectedIndex()
            {
                NotifyPropertyChanged("SelectedIndex");
            }

            public void UpdateSongListAndSelectedIndex()
            {
                isUpdatingSongList = true;
                UpdateSelectedIndex();
                NotifyPropertyChanged("SongList");
                isUpdatingSongList = false;
                UpdateSelectedIndex();
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void NotifyPropertyChanged(string propertyName)
            {
                try
                {
                    if (null == PropertyChanged) return;

                    if (parent.Dispatcher.HasThreadAccess) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    else
                    {
                        parent.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
                    }
                }
                catch { }
            }
        }
    }
}
