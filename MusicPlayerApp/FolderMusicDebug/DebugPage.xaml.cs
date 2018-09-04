using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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

            DataContext = ViewModel.Current;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Ref_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Current.Reload();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(ViewModel.DebugDataFilepath);
                await file.DeleteAsync();
            }
            catch { }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DebugFilterPage));
        }
    }
}
