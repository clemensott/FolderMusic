using FolderMusicLib;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TagLib;
using Windows.Storage;
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
            get { return title; }
            set { title = value; }
        }

        public string Artist
        {
            get { return artist; }
            set { artist = value; }
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        [XmlIgnore]
        public Brush TextBrush { get { return Playlist.TextBrush; } }

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

        public async Task Refresh()
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
            File tagFile;
            Tag tags;
            Stream fileStream;

            try
            {
                fileStream = await file.OpenStreamForReadAsync();

                tagFile = File.Create(new StreamFileAbstraction(file.Name, fileStream, fileStream));
                tags = tagFile.GetTag(TagTypes.Id3v2);

                if (tags == null || tags.IsEmpty) return;

                title = tags != null && tags.Title != null && tags.Title != "" ? tags.Title : GetTitleFromPath();
                artist = tags != null && tags.FirstPerformer != null ? tags.FirstPerformer : "";
            }
            catch { }
        }

        private void SetTitleAndArtistByPath()
        {
            title = GetTitleFromPath();
            artist = "Unkown";
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
