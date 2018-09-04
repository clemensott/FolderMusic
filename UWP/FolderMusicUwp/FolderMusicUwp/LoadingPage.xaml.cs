using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace FolderMusicUwp
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class LoadingPage : Page
    {
        private static bool open;
        private static LoadingPage page;

        public static bool Open { get { return open; } }

        public LoadingPage()
        {
            this.InitializeComponent();

            if (open) GoBack();

            open = true;
            page = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        public async static Task NavigateTo()
        {
            if (Open) return;

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.
                RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    (Window.Current.Content as Frame).Navigate(typeof(LoadingPage));
                });
        }

        public static void GoBack()
        {
            if (!open) return;

            open = false;

            if (LibraryLib.Library.Current.CanceledLoading) LibraryLib.Library.Current.CancelLoading();

            page.Frame.GoBack();
        }
    }
}
