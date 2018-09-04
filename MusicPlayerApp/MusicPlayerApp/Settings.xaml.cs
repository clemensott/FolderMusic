using FolderMusicLib;
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

namespace MusicPlayerApp
{
    public sealed partial class Settings : Page
    {
        private static readonly string dataPath = ApplicationData.Current.LocalFolder.Path + "\\Times.txt";

        public Settings()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            string[] lines = new string[3];

            lines[0] = cbxIsOn.IsChecked.ToString();
            lines[1] = tbxPeriodeTime.Text;
            lines[2] = "";

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("Times.txt");
            }
            catch { }

            await PathIO.WriteLinesAsync(dataPath, lines);

            Frame.GoBack();

            BackgroundCommunicator.SendRingerChanged();
        }
    }
}
