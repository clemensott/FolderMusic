using MusicPlayer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace FolderMusic
{
    public sealed partial class SkipSongsPage : Page
    {
        private static volatile bool open = false;

        public static bool Open => open;

        private SkipSongs list;

        public SkipSongsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            open = true;
            list = e.Parameter as SkipSongs;
            list.SkippedSong += List_SkippedSong;

            lbxSongs.ItemsSource = list.GetSongs();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            open = false;

            base.OnNavigatedFrom(e);
        }

        private void List_SkippedSong(object sender, EventArgs e)
        {
            IList<object> selectedItems = lbxSongs.SelectedItems;

            lbxSongs.ItemsSource = list.GetSongs();

            foreach (object selectedItem in selectedItems) lbxSongs.SelectedItems.Add(selectedItem);
        }

        private void Keep_Click(object sender, RoutedEventArgs e)
        {
            foreach (SkipSong skipSong in list)
            {
                skipSong.Handle = lbxSongs.SelectedItems.Contains(skipSong.Song) ? HandleType.Keep : HandleType.Skip;
            }

            lbxSongs.ItemsSource = list.GetSongs();

            if (lbxSongs.Items.Count == 0) Frame.GoBack();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            foreach (SkipSong skipSong in list)
            {
                skipSong.Handle = lbxSongs.SelectedItems.Contains(skipSong.Song) ? HandleType.Remove : HandleType.Skip;
            }

            lbxSongs.ItemsSource = list.GetSongs();

            if (lbxSongs.Items.Count == 0) Frame.GoBack();
        }

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            object[] unselectedItems = lbxSongs.Items.Except(lbxSongs.SelectedItems).ToArray();

            lbxSongs.SelectedItems.Clear();

            foreach (object item in unselectedItems) lbxSongs.SelectedItems.Add(item);
            //if (lbxSongs.SelectedItems.Count == 0) lbxSongs.SelectAll();
            //else lbxSongs.SelectedItems.Clear();
        }
    }
}
