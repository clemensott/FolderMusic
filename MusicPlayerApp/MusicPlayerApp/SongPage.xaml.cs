using MusicPlayer.Data;
using System;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkID=390556 dokumentiert.

namespace FolderMusic
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SongPage : Page
    {
        private Song song;

        public SongPage()
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
            song = e.Parameter as Song;

            if (song == null) return;

            StorageFile.GetFileFromPathAsync(song.Path).Completed = LoadedStorageFile;
        }

        private void LoadedStorageFile(IAsyncOperation<StorageFile> asyncInfo, AsyncStatus asyncStatus)
        {
            StorageFile file = asyncInfo.GetResults();

            file.Properties.GetMusicPropertiesAsync().Completed = LoadedMusicProperties;
        }

        private async void LoadedMusicProperties(IAsyncOperation<MusicProperties> asyncInfo, AsyncStatus asyncStatus)
        {
            if (asyncStatus == AsyncStatus.Completed)
            {
                MusicProperties props = asyncInfo.GetResults();

                Utils.DoSafe(() => { DataContext = props; });
            }
            else if (asyncStatus == AsyncStatus.Error)
            {
                string message = "Loading Properties failed.\n" + asyncInfo.ErrorCode.Message;
                MessageDialog dialog = new MessageDialog(message);

                await dialog.ShowAsync();
            }
            else if (asyncStatus == AsyncStatus.Canceled)
            {
                string message = "Loading Properties has been canceled.";
                MessageDialog dialog = new MessageDialog(message);

                await dialog.ShowAsync();
            }
        }

        private async void AbbSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MusicProperties props = (MusicProperties)DataContext;

                await props.SavePropertiesAsync();
                await song?.Reset();
            }
            catch (Exception exc)
            {
                await new MessageDialog(exc.Message, exc.GetType().Name).ShowAsync();
            }
        }
    }
}
