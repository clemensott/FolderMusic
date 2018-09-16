using System;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace MusicPlayer.Data
{
    public delegate void TitlePropertyChangedEventHandler(Song sender, SongTitleChangedEventArgs args);
    public delegate void ArtistPropertyChangedEventHandler(Song sender, SongArtistChangedEventArgs args);
    public delegate void DurationPropertyChangedEventHandler(Song sender, SongDurationChangedEventArgs args);

    public class Song : IXmlSerializable
    {
        public const double DefaultDuration = 400;

        public static Song GetEmpty(ISongCollection parent)
        {
            return new Song(parent);
        }

        public event TitlePropertyChangedEventHandler TitleChanged;
        public event ArtistPropertyChangedEventHandler ArtistChanged;
        public event DurationPropertyChangedEventHandler DurationChanged;

        private bool failed;
        private double durationMilliseconds;
        private string title, artist, path;

        public bool IsEmpty { get { return path == string.Empty; } }

        public bool Failed { get { return failed; } }

        public ISongCollection Parent { get; set; }

        public double DurationMilliseconds
        {
            get { return !double.IsNaN(durationMilliseconds) ? durationMilliseconds : DefaultDuration; }
            set
            {
                if (value < DefaultDuration || value == durationMilliseconds) return;

                var args = new SongDurationChangedEventArgs(durationMilliseconds, value);
                durationMilliseconds = value;
                DurationChanged?.Invoke(this, args);
            }
        }

        public string Title
        {
            get { return title == string.Empty ? GetTitleFromPath() : title; }
            set
            {
                if (value == title) return;

                var args = new SongTitleChangedEventArgs(title, value);
                title = value;
                TitleChanged?.Invoke(this, args);
            }
        }

        public string Artist
        {
            get { return artist == string.Empty ? "Unkown" : artist; }
            set
            {
                if (value == artist) return;

                var args = new SongArtistChangedEventArgs(artist, value);
                artist = value;
                ArtistChanged?.Invoke(this, args);
            }
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        private Song(ISongCollection parent)
        {
            failed = true;

            durationMilliseconds = double.NaN;

            title = "Empty";
            artist = path = string.Empty;

            Parent = parent;
        }

        internal Song(ISongCollection parent, CurrentPlaySong currentPlaySong)
        {
            Parent = parent;
            failed = false;

            durationMilliseconds = DefaultDuration;
            path = currentPlaySong.Path;
            title = currentPlaySong.Title;
            artist = currentPlaySong.Artist;
        }

        internal Song(ISongCollection parent, string xmlText)
        {
            Parent = parent;
            ReadXml(XmlConverter.GetReader(xmlText));
        }

        private Song(ISongCollection parent, double durationMilliseconds, string path, string title, string artist)
        {
            failed = false;

            this.durationMilliseconds = durationMilliseconds;
            this.path = path;
            this.title = title;
            this.artist = artist;

            Parent = parent;
        }

        public static Song GetLoaded(ISongCollection parent, StorageFile file)
        {
            Task<MusicProperties> task = file.Properties.GetMusicPropertiesAsync().AsTask();
            task.Wait();
            MusicProperties properties = task.Result;

            string title = properties.Title;
            string artist = properties.Artist;
            double durationMilliseconds = properties.Duration.TotalMilliseconds;

            return new Song(parent, durationMilliseconds, file.Path, title, artist);
        }

        public async Task Reset()
        {
            durationMilliseconds = DefaultDuration;

            try
            {
                StorageFile file = await GetStorageFileAsync();
                await SetTitleAndArtist(file);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SongResetFail", e, Path);
            }
        }

        private async Task SetTitleAndArtist(StorageFile file)
        {
            try
            {
                MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                if (properties == null) return;

                Title = properties.Title;
                Artist = properties.Artist;
                DurationMilliseconds = properties.Duration.TotalMilliseconds;
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SongSetTitleAndArtistFail", e, Path);
            }
        }

        public async Task LoadDuration()
        {
            try
            {
                MusicProperties properties = await (await GetStorageFileAsync()).Properties.GetMusicPropertiesAsync();

                if (properties == null) return;

                DurationMilliseconds = properties.Duration.TotalMilliseconds;
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SongLoadDurationFail", e, Path);
            }
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
                MobileDebug.Service.WriteEvent("SongGetFileFail", e, Path);
                failed = true;
                throw e;
            }
        }

        public void SetFailed()
        {
            failed = true;
        }

        public override bool Equals(object obj)
        {
            return this == (Song)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return Artist != null && Artist != string.Empty ? Artist + " - " + Title : Title;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            DurationMilliseconds = double.Parse(reader.GetAttribute("DurationMilliseconds"));
            Title = reader.GetAttribute("Title");
            Artist = reader.GetAttribute("Artist");
            Path = reader.GetAttribute("Path");
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("DurationMilliseconds", DurationMilliseconds.ToString());
            writer.WriteAttributeString("Title", Title.ToString());
            writer.WriteAttributeString("Artist", Artist.ToString());
            writer.WriteAttributeString("Path", Path);
        }

        public static bool operator ==(Song song1, Song song2)
        {
            if (ReferenceEquals(song1, song2)) return true;
            if (ReferenceEquals(song1, null) || ReferenceEquals(song2, null)) return false;
            if (song1.Artist != song2.Artist) return false;
            if (song1.Title != song2.Title) return false;
            if (song1.DurationMilliseconds != song2.DurationMilliseconds) return false;
            if (song1.Path != song2.Path) return false;

            return true;
        }

        public static bool operator !=(Song song1, Song song2)
        {
            return !(song1 == song2);
        }
    }
}
