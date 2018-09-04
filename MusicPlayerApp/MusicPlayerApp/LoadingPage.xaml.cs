using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkID=390556 dokumentiert.

namespace MusicPlayerApp
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Rahmens navigiert werden kann.
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
