using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TagLib;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace LibraryLib
{
    public class Song : INotifyPropertyChanged
    {
        private bool isLoading;
        private double naturalDurationMilliseconds;
        private string title, artist, path;

        [XmlIgnore]
        public bool IsEmptyOrLoading { get { return path == "" || isLoading; } }

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

        [XmlIgnore]
        public Visibility ArtistVisibility { get { return artist != "" ? Visibility.Visible : Visibility.Collapsed; } }

        public Song()
        {
            isLoading = false;
            SetEmptyOrLoading();
        }

        public Song(string absolutePath)
        {
            isLoading = true;
            path = absolutePath;
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

                if (tags == null || tags.IsEmpty)
                {
                    SetTitleAndArtistByPath();
                    return;
                }

                title = tags != null && tags.Title != null && tags.Title != "" ? tags.Title : GetTitleFromPath();
                artist = tags != null && tags.FirstPerformer != null ? tags.FirstPerformer : "";
            }
            catch
            {
                SetTitleAndArtistByPath();
            }
        }

        private void SetTitleAndArtistByPath()
        {
            title = GetTitleFromPath();
            artist = "Unkown";
        }

        private string GetTitleFromPath()
        {
            return System.IO.Path.GetFileNameWithoutExtension(Path);
        }

        public StorageFile GetStorageFile()
        {
            Task<StorageFile> storageFileTask = GetStorageFileAsync();
            storageFileTask.Wait();

            return storageFileTask.Result;
        }

        public async Task<StorageFile> GetStorageFileAsync()
        {
            return await StorageFile.GetFileFromPathAsync(Path);
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
            return Artist != "" ? Artist + " - " + Title : Title;
        }
    }
}
