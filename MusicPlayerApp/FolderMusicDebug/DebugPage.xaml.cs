using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkID=390556 dokumentiert.

namespace FolderMusicDebug
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class DebugPage : Page
    {
        public DebugPage()
        {
            this.InitializeComponent();

            DataContext = ViewModelDebug.Current;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Ref_Click(object sender, RoutedEventArgs e)
        {
            ViewModelDebug.Current.Reload();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await PathIO.WriteTextAsync(ViewModelDebug.DebugDataFilepath, string.Empty);
            }
            catch { }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DebugFilterPage));
        }
    }
}
