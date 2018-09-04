using LibraryLib;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace FolderMusicUwpLib
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SkipSongsPage : Page
    {
        private static volatile bool open = false;
        private static SkipSongsPage page;

        public static bool Open { get { return open; } }

        private SkipSongs skipSongs;

        public SkipSongsPage()
        {
            this.InitializeComponent();
            page = this;
        }

        public static async Task NavigateToIfSkipSongsExists()
        {
            if (!Open && Library.IsLoaded)
            {
                open = true;
                SkipSongs skipSongs = await SkipSongs.GetNew();

                if (skipSongs.HaveSongs)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.
                        RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            (Window.Current.Content as Frame).Navigate(typeof(SkipSongsPage), skipSongs);
                        });
                }
                else open = false;
            }
        }

        public static void GoBack()
        {
            if (!Open) return;

            open = false;
            page.Frame.GoBack();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            skipSongs = e.Parameter as SkipSongs;
            SetCurrentSongPath();
        }

        private void SetCurrentSongPath()
        {
            if (skipSongs.CurrentSong.IsEmptyOrLoading) GoBack();
            else tbl_Path.Text = skipSongs.CurrentSong.RelativePath;
        }

        private async void Yes_Click(object sender, RoutedEventArgs e)
        {
            await skipSongs.Yes_Click();
            SetCurrentSongPath();
        }

        private async void No_Click(object sender, RoutedEventArgs e)
        {
            await skipSongs.No_Click();
            SetCurrentSongPath();
        }

        private async void Skip_Click(object sender, RoutedEventArgs e)
        {
            await skipSongs.Skip_Click();
            SetCurrentSongPath();
        }
    }
}
