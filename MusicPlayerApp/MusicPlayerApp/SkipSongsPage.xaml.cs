using MusicPlayer.Data;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace FolderMusic
{
    public sealed partial class SkipSongsPage : Page
    {
        private static volatile bool open = false;

        public static bool Open { get { return open; } }

        private SkipSongs skipSongs;

        public SkipSongsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!Library.Current.SkippedSongs.MoveNext()) Frame.GoBack();
            else SetCurrentSongPath();
        }

        private void SetCurrentSongPath()
        {
            do
            {
                if (!Library.Current.SkippedSongs.Current.Song.IsEmptyOrLoading)
                {
                    tbl_Path.Text = Library.Current.SkippedSongs.Current.Song.RelativePath;
                }

            } while (Library.Current.SkippedSongs.MoveNext());

            Library.Current.SkippedSongs.Dispose();
            Frame.GoBack();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Library.Current.SkippedSongs.Current.Handle = ProgressType.Remove;

            if (!Library.Current.SkippedSongs.MoveNext()) Frame.GoBack();

            SetCurrentSongPath();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Library.Current.SkippedSongs.Current.Handle = ProgressType.Leave;

            if (!Library.Current.SkippedSongs.MoveNext()) Frame.GoBack();

            SetCurrentSongPath();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            Library.Current.SkippedSongs.Current.Handle = ProgressType.Skip;

            if (!Library.Current.SkippedSongs.MoveNext()) Frame.GoBack();

            SetCurrentSongPath();
        }
    }
}
