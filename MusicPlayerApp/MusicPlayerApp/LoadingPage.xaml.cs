using MusicPlayer;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkID=390556 dokumentiert.

namespace FolderMusic
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class LoadingPage : Page
    {
        private StopOperationToken stopToken;

        public LoadingPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            stopToken = (StopOperationToken)e.Parameter;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            stopToken.Stopped -= CancelToken_Stopped;
            stopToken.Stop();

            base.OnNavigatingFrom(e);
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            stopToken.Stopped += CancelToken_Stopped;

            if (stopToken.IsStopped) Frame.GoBack();
        }

        private void CancelToken_Stopped(object sender, System.EventArgs e)
        {
            Frame.GoBack();
        }
    }
}
