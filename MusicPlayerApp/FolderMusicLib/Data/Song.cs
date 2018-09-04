using System;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace MusicPlayer.Data
{
    public class Song
    {
        public const double DefaultDuration = 400;
        private bool isLoading, failed = false;
        private double naturalDurationMilliseconds = double.NaN;
        private string title, artist, path;

        [XmlIgnore]
        public bool IsEmptyOrLoading { get { return path == string.Empty || isLoading; } }

        [XmlIgnore]
        public bool Failed { get { return failed; } }

        public double NaturalDurationMilliseconds
        {
            get
            {
                //if (!double.IsNaN(naturalDurationMilliseconds) || naturalDurationMilliseconds == 1) LoadNaturalDuration();

                return !double.IsNaN(naturalDurationMilliseconds) ? naturalDurationMilliseconds : DefaultDuration;
            }
            set
            {
                if (value < 1 || value == naturalDurationMilliseconds) return;

                double oldValue = naturalDurationMilliseconds;
                naturalDurationMilliseconds = value;

                Feedback.Current.RaiseNaturalDurationPropertyChanged(this, oldValue, value);
            }
        }

        public string Title
        {
            get { return title == string.Empty ? GetTitleFromPath() : title; }
            set
            {
                if (value == title) return;

                string oldValue = title;
                title = value;

                Feedback.Current.RaiseTitlePropertyChanged(this, oldValue, value);
            }
        }

        public string Artist
        {
            get { return artist == string.Empty ? "Unkown" : artist; }
            set
            {
                if (value == artist) return;

                string oldValue = artist;
                artist = value;

                Feedback.Current.RaiseArtistPropertyChanged(this, oldValue, value);
            }
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        [XmlIgnore]
        public string RelativePath { get { return Playlist.GetRelativePath(path); } }

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
            //Title = Library.IsLoaded() ? "Empty" : "Loading";
            if (Library.IsLoaded()) Title = "Empty";
            else Title = "Loading";
            Artist = path = string.Empty;
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
        }

        private async Task SetTitleAndArtist(StorageFile file)
        {
            try
            {
                MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                if (properties == null) return;

                Title = properties.Title;
                Artist = properties.Artist;
                NaturalDurationMilliseconds = properties.Duration.TotalMilliseconds;
            }
            catch { }
        }

        public async Task LoadNaturalDuration()
        {
            try
            {
                MusicProperties properties = await (await GetStorageFileAsync()).Properties.GetMusicPropertiesAsync();

                if (properties == null) return;

                NaturalDurationMilliseconds = properties.Duration.TotalMilliseconds;
            }
            catch { }
        }

        private void SetTitleAndArtistByPath()
        {
            Title = GetTitleFromPath();
            Artist = string.Empty;
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

        private void SaveFailed()
        {
            //string filename = "SongFailed.txt";
            //string text = DateTime.Now.Ticks.ToString() + ";" + RelativePath + "\n";

            //IO.AppendText(text, filename);
        }

        public override bool Equals(object obj)
        {
            return this == obj as Song;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Song song1, Song song2)
        {
            if (ReferenceEquals(song1, song2)) return true;
            if (ReferenceEquals(song1, null) || ReferenceEquals(song2, null)) return false;
            if (song1.Artist != song2.Artist) return false;
            if (song1.Title != song2.Title) return false;
            if (song1.NaturalDurationMilliseconds != song2.NaturalDurationMilliseconds) return false;
            if (song1.Failed != song2.Failed) return false;
            if (song1.Path != song2.Path) return false;

            return true;
        }

        public static bool operator !=(Song song1, Song song2)
        {
            return !(song1 == song2);
        }

        public override string ToString()
        {
            return Artist != null && Artist != string.Empty ? Artist + " - " + Title : Title;
        }
    }
}
