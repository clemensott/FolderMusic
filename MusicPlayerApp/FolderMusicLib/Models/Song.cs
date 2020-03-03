using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.FileProperties;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models
{
    public class Song : INotifyPropertyChanged, IXmlSerializable
    {
        public const double DefaultDuration = 400;

        public event EventHandler<SongTitleChangedEventArgs> TitleChanged;
        public event EventHandler<SongArtistChangedEventArgs> ArtistChanged;
        public event EventHandler<SongDurationChangedEventArgs> DurationChanged;

        private bool failed;
        private double durationMilliseconds;
        private string title, artist, path;

        public bool IsEmpty => path == string.Empty;

        public bool Failed => failed;

        public ISongCollection Parent { get; set; }

        public double DurationMilliseconds
        {
            get { return !double.IsNaN(durationMilliseconds) ? durationMilliseconds : DefaultDuration; }
            set
            {
                if (value < DefaultDuration || value == durationMilliseconds) return;

                SongDurationChangedEventArgs args = new SongDurationChangedEventArgs(durationMilliseconds, value);
                durationMilliseconds = value;
                DurationChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(DurationMilliseconds));
            }
        }

        public string Title
        {
            get { return title == string.Empty ? GetTitleFromPath() : title; }
            set
            {
                if (value == title) return;

                SongTitleChangedEventArgs args = new SongTitleChangedEventArgs(title, value);
                title = value;
                TitleChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Artist
        {
            get { return artist == string.Empty ? "Unkown" : artist; }
            set
            {
                if (value == artist) return;

                SongArtistChangedEventArgs args = new SongArtistChangedEventArgs(artist, value);
                artist = value;
                ArtistChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(Artist));
            }
        }

        public string Path
        {
            get { return path; }
            set
            {
                if (value == path) return;

                path = value;
                OnPropertyChanged(nameof(DurationMilliseconds));
            }
        }

        public Song()
        {
            failed = true;

            durationMilliseconds = double.NaN;

            title = "Empty";
            artist = path = string.Empty;
        }

        internal Song(CurrentPlaySong currentPlaySong)
        {
            failed = false;

            durationMilliseconds = DefaultDuration;
            path = currentPlaySong.Path;
            title = currentPlaySong.Title;
            artist = currentPlaySong.Artist;
        }

        private Song(double durationMilliseconds, string path, string title, string artist)
        {
            failed = false;

            this.durationMilliseconds = durationMilliseconds;
            this.path = path;
            this.title = title;
            this.artist = artist;
        }

        public static async Task<Song> GetLoaded(StorageFile file)
        {
            MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

            string title = properties.Title;
            string artist = properties.Artist;
            double durationMilliseconds = properties.Duration.TotalMilliseconds;

            return new Song(durationMilliseconds, file.Path, title, artist);
        }

        public async Task Reset()
        {
            durationMilliseconds = DefaultDuration;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(Path);
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
                StorageFile file = await StorageFile.GetFileFromPathAsync(Path);
                MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

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

        public void SetFailed()
        {
            failed = true;
        }

		public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (ReferenceEquals(obj, null)) return false;

            if (!(obj is Song)) return false;

            Song other = (Song)obj;

            if (Artist != other.Artist) return false;
            if (Title != other.Title) return false;
            if (DurationMilliseconds != other.DurationMilliseconds) return false;
            if (Path != other.Path) return false;

            return true;
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
            failed = false;

            reader.ReadStartElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("DurationMilliseconds", DurationMilliseconds.ToString());
            writer.WriteAttributeString("Title", Title);
            writer.WriteAttributeString("Artist", Artist);
            writer.WriteAttributeString("Path", Path);
        }

        public static bool operator ==(Song song1, Song song2)
        {
            return (song1?.Equals(song2) ?? song2?.Equals(song1)) ?? true;
        }

        public static bool operator !=(Song song1, Song song2)
        {
            return !(song1 == song2);
        }
    }
}
