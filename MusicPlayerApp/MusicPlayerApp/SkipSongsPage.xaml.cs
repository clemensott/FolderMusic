using MusicPlayer.Data;
using System.Collections.Generic;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace FolderMusic
{
    public sealed partial class SkipSongsPage : Page
    {
        private static volatile bool open = false;

        public static bool Open { get { return open; } }

        private IEnumerator<SkipSong> enumerator;

        public SkipSongsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            IEnumerable<SkipSong> skippedSongs = e.Parameter as IEnumerable<SkipSong>;
            enumerator = skippedSongs?.GetEnumerator();

            if (enumerator?.MoveNext() != true) Frame.GoBack();
            else SetCurrentSongPath();
        }

        private void SetCurrentSongPath()
        {
            do
            {
                if (!enumerator.Current.Song.IsEmpty)
                {
                    tbl_Path.Text = enumerator.Current.Song.Path;
                }

            } while (enumerator.MoveNext());

            enumerator.Dispose();
            Frame.GoBack();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            enumerator.Current.Handle = ProgressType.Remove;

            if (!enumerator.MoveNext()) Frame.GoBack();

            SetCurrentSongPath();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            enumerator.Current.Handle = ProgressType.Leave;

            if (!enumerator.MoveNext()) Frame.GoBack();

            SetCurrentSongPath();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            enumerator.Current.Handle = ProgressType.Skip;

            if (!enumerator.MoveNext()) Frame.GoBack();

            SetCurrentSongPath();
        }
    }
}
