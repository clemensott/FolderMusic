using FolderMusicUwpLib;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;

namespace LibraryLib
{
    public sealed class Song : INotifyPropertyChanged
    {
        private bool isLoading, failed = false;
        private double naturalDurationMilliseconds = 1;
        private string title, artist, path;

        [XmlIgnore]
        public bool IsEmptyOrLoading { get { return path == "" || isLoading; } }

        [XmlIgnore]
        public bool Failed { get { return failed; } }

        public double NaturalDurationMilliseconds
        {
            get { return naturalDurationMilliseconds; }
            set { naturalDurationMilliseconds = value; }
        }

        public string Title
        {
            get { return title == "" ? GetTitleFromPath() : title; }
            set { title = value; }
        }

        public string Artist
        {
            get { return artist == "" ? "Unkown" : artist; }
            set { artist = value; }
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        [XmlIgnore]
        public string RelativePath { get { return Playlist.GetRelativePath(path); } }

        [XmlIgnore]
        public Brush TextFirstBrush { get { return Playlist.TextFirstBrush; } }

        [XmlIgnore]
        public Brush TextSecondBrush { get { return Playlist.TextSecondBrush; } }

        public Song()
        {
            isLoading = false;
            SetEmptyOrLoading();
        }

        public Song(string absolutePath)
        {
            isLoading = true;
            path = absolutePath;

            SetTitleAndArtistByPath();
        }

        private void SetEmptyOrLoading()
        {
            title = Library.IsLoaded ? "Empty" : "Loading";
            artist = path = "";
        }

        public async void Refresh()
        {
            naturalDurationMilliseconds = 1;

            try
            {
                StorageFile file = await GetStorageFileAsync();
                await SetTitleAndArtist(file);
            }
            catch
            {
                SetEmptyOrLoading();
            }

            isLoading = false;

            BackgroundCommunicator.SendSongXML(this);
            UpdateTitleAndArtist();
        }

        private async Task SetTitleAndArtist(StorageFile file)
        {
            try
            {
                MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();
               
                title = properties != null && properties.Title != null ? properties.Title : "";
                artist = properties != null && properties.Artist != null ? properties.Artist : "";

                if (properties != null) naturalDurationMilliseconds = properties.Duration.TotalMilliseconds;
            }
            catch { }
        }

        private void SetTitleAndArtistByPath()
        {
            title = GetTitleFromPath();
            artist = "";
        }

        private string GetTitleFromPath()
        {
            return System.IO.Path.GetFileName(Path);
        }

        public StorageFile GetStorageFile()
        {
            Task<StorageFile> storageFileTask = GetStorageFileAsync();
            storageFileTask.Wait();

            return storageFileTask.Result;
        }

        public async Task<StorageFile> GetStorageFileAsync()
        {
            try
            {
                return await StorageFile.GetFileFromPathAsync(Path);
            }
            catch (Exception e)
            {
                failed = true;
                throw e;
            }
        }

        public void SetFailed()
        {
            failed = true;
            SaveFailed();
        }

        private async void SaveFailed()
        {
            string filename = "SongFailed.txt";
            string text = "";

            try
            {
                text = await LibraryIO.LoadText(filename) + "\n";
            }
            catch { }

            text += DateTime.Now.Ticks.ToString() + ";" + RelativePath;

            await LibraryIO.SaveText(text, filename);
        }

        public void UpdateTitleAndArtist()
        {
            NotifyPropertyChanged("Title");
            NotifyPropertyChanged("Artist");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                if (null == PropertyChanged) return;

                await Windows.ApplicationModel.Core.CoreApplication.MainView.
                    CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
            }
            catch { }
        }

        public override string ToString()
        {
            return Artist != null && Artist != "" ? Artist + " - " + Title : Title;
        }
    }
}
