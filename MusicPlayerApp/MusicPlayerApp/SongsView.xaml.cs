using FolderMusic.Converters;
using MusicPlayer;
using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace FolderMusic
{
    public abstract partial class SongsView : UserControl
    {
        enum ScrollToType { No, Last, Current }

        public enum SongsSourceType { Default, Shuffle }

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

            s.scrollTo = ScrollToType.Last;
        }

        private IShuffleCollection showShuffleSongs;
        private ScrollToType scrollTo;

        public IPlaylist Source
        {
            get { return (IPlaylist)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public SongsView()
        {
            this.InitializeComponent();

            Binding itemsSourceBinding = new Binding()
            {
                Converter = GetConverter(),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            Binding selectedItemBinding = new Binding()
            {
                Path = new PropertyPath("CurrentSong"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            lbxSongs.SetBinding(ListBox.ItemsSourceProperty, itemsSourceBinding);
            lbxSongs.SetBinding(ListBox.SelectedItemProperty, selectedItemBinding);
        }

        private void control_Loaded(object sender, RoutedEventArgs e)
        {
            scrollTo = ScrollToType.Last;
        }

        protected abstract IValueConverter GetConverter();

        private void Subscibe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongPropertyChanged;
        }

        private void Unsubscibe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongPropertyChanged;
        }

        private void OnCurrentSongPropertyChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess) SetSelectedItem();
            else CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, SetSelectedItem);
        }

        private void SetSelectedItem()
        {
            lbxSongs.SelectedItem = Source.CurrentSong;
        }

        private void OnShuffleSongsChanged(IShuffleCollection sender)
        {
            ScrollToCurrentSongTop();
        }

        public void ScrollToCurrentSongTop()
        {
            try
            {
                MobileDebug.Manager.WriteEvent("ScrollToCurrentTop", lbxSongs.SelectedItem);

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
                lbxSongs.ScrollIntoView(Source.CurrentSong);
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
    }
}
