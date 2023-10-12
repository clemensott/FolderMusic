using MusicPlayer.UpdateLibrary;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkID=390556 dokumentiert.

namespace FolderMusic
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class UpdateProgressPage : Page
    {
        private BaseUpdateProgress progress;

        public UpdateProgressPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Wird aufgerufen, wenn diese Seite in einem Frame angezeigt werden soll.
        /// </summary>
        /// <param name="e">Ereignisdaten, die beschreiben, wie diese Seite erreicht wurde.
        /// Dieser Parameter wird normalerweise zum Konfigurieren der Seite verwendet.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            progress = (BaseUpdateProgress)e.Parameter;
            gidMain.DataContext = progress;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            progress.CancelToken.Finished -= CancelToken_Finished;
            progress.CancelToken.Cancel();

            base.OnNavigatingFrom(e);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (progress.CancelToken.Result.HasValue) Frame.GoBack();
            else progress.CancelToken.Finished += CancelToken_Finished;
        }

        private void CancelToken_Finished(object sender, CancelTokenResult e)
        {
            Frame.GoBack();
        }

        private object ChildVisibilityCon_ConvertEvent(object value, Type targetType, object parameter, string language)
        {
            return value is BaseUpdateProgress ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
