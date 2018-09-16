using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkID=390556 dokumentiert.

namespace FolderMusic
{
    public sealed partial class SettingsPage : Page
    {
        private const string dataFileName = "Times.txt";

        public SettingsPage()
        {
            this.InitializeComponent();

            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(dataFileName);
                IList<string> lines = await FileIO.ReadLinesAsync(file);

                cbxIsOn.IsChecked = bool.Parse(lines[0]);
                tbxPeriodeTime.Text = lines[0];
            }
            catch { }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private async void SaveAndBack_Click(object sender, RoutedEventArgs e)
        {
            string[] lines = new string[3];

            lines[0] = cbxIsOn.IsChecked.ToString();
            lines[1] = tbxPeriodeTime.Text;
            lines[2] = string.Empty;

            try
            {
                StorageFile file;

                try
                {
                    file = await ApplicationData.Current.LocalFolder.GetFileAsync(dataFileName);
                }
                catch (FileNotFoundException)
                {
                    file = await ApplicationData.Current.LocalFolder.CreateFileAsync("Times.txt");
                }

                await FileIO.WriteLinesAsync(file, lines);
            }
            catch (Exception exc)
            {
                string message = "Didn't save.\n" + exc.Message;
                await new MessageDialog(message,exc.GetType().Name).ShowAsync();
            }

            Frame.GoBack();
        }
    }
}
