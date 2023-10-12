using System;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using FolderMusic.NavigationParameter;
using MusicPlayer;
using MusicPlayer.Models;
using MusicPlayer.Models.Foreground.Interfaces;
using MusicPlayer.UpdateLibrary;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkID=390556 dokumentiert.

namespace FolderMusic
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb Page_Loadedigiert werden kann.
    /// </summary>
    public sealed partial class SongPage : Page
    {
        private Song song;
        private ISongCollection songs;
        private StorageFile file;

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
            SongPageParameter parameter = (SongPageParameter)e.Parameter;
            song = parameter.Song;
            songs = parameter.Songs;

            tblPath.Text = song.FullPath;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                file = await StorageFile.GetFileFromPathAsync(song.FullPath);
                DataContext = await file.Properties.GetMusicPropertiesAsync();
            }
            catch (Exception exc)
            {
                await new MessageDialog(exc.Message, "Load song data error").ShowAsync();
                Frame.GoBack();
            }
        }

        private async void AbbSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MusicProperties props = (MusicProperties)DataContext;

                await props.SavePropertiesAsync();

                Song? newSong = await UpdateLibraryUtils.LoadSong(file);
                if (newSong.HasValue)
                {
                    Song oldSong;
                    if (songs.TryGetSong(newSong.Value.FullPath, out oldSong))
                    {
                        if (!Equals(newSong.Value, oldSong))
                        {
                            songs.Change(new Song[] {oldSong}, new Song[] {newSong.Value});
                        }
                    }
                    else await new MessageDialog("Song not found in playlist").ShowAsync();
                }
                else await new MessageDialog("Reloading song failed").ShowAsync();
            }
            catch (Exception exc)
            {
                await new MessageDialog(exc.Message, exc.GetType().Name).ShowAsync();
            }
        }
    }
}
