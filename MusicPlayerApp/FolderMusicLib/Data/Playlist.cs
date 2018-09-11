using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Windows.Storage;

namespace MusicPlayer.Data
{
    class Playlist : IPlaylist
    {
        private const string emptyName = "None", emptyOrLoadingPath = "None";

        public event CurrentSongPropertyChangedEventHandler CurrentSongChanged;
        public event CurrentSongPositionPropertyChangedEventHandler CurrentSongPositionChanged;
        public event ShufflePropertyChangedEventHandler ShuffleChanged;
        public event LoopPropertyChangedEventHandler LoopChanged;

        private double currentSongPositionPercent = double.NaN;
        private string name = emptyName, absolutePath = emptyOrLoadingPath;
        private Song currentSong;
        private LoopType loop = LoopType.Off;

        public Song this[int index] { get { return Songs.ElementAt(index); } }

        public bool IsEmpty { get { return Songs.Count == 0; } }

        public int SongsCount { get { return Songs.Count; } }

        public double CurrentSongPositionPercent
        {
            get
            {
                return !double.IsNaN(currentSongPositionPercent) ? currentSongPositionPercent : Library.DefaultSongsPositionPercent;
            }
            set
            {
                if (value == currentSongPositionPercent) return;

                double oldCurrentSongPositionPercent = currentSongPositionPercent;
                currentSongPositionPercent = value;

                var args = new CurrentSongPositionChangedEventArgs(oldCurrentSongPositionPercent, currentSongPositionPercent);
                CurrentSongPositionChanged?.Invoke(this, args);
            }
        }

        public string Name
        {
            get { return name; }
        }

        public string AbsolutePath
        {
            get { return absolutePath; }
        }

        public Song CurrentSong
        {
            get { return currentSong; }
            set
            {
                if (value == currentSong || !Songs.Contains(value)) return;

                var args = new CurrentSongChangedEventArgs(currentSong, value);
                currentSong = value;
                currentSongPositionPercent = 0;
                CurrentSongChanged?.Invoke(this, args);
            }
        }

        public ISongCollection Songs { get; private set; }

        public IShuffleCollection ShuffleSongs { get; private set; }

        public LoopType Loop
        {
            get { return loop; }
            set
            {
                if (value == loop) return;

                LoopType oldType = loop;
                loop = value;

                var args = new LoopChangedEventArgs(oldType, loop);
                LoopChanged?.Invoke(this, args);
            }
        }

        internal ILoop Looper { get { return GetLooper(Loop); } }

        public ShuffleType Shuffle
        {
            get { return ShuffleSongs?.Type ?? ShuffleType.Off; }
            set
            {
                if (ShuffleSongs != null && value == Shuffle) return;

                IShuffleCollection oldShuffleSongs = ShuffleSongs;

                switch (value)
                {
                    case ShuffleType.Off:
                        ShuffleSongs = new ShuffleOffCollection(this, Songs);
                        break;

                    case ShuffleType.OneTime:
                        ShuffleSongs = new ShuffleOneTimeCollection(this, Songs, CurrentSong);
                        break;

                    case ShuffleType.Complete:
                        ShuffleSongs = new ShuffleCompleteCollection(this, Songs, CurrentSong);
                        break;
                }

                var args = new ShuffleChangedEventArgs(oldShuffleSongs, ShuffleSongs, CurrentSong, CurrentSong);
                ShuffleChanged?.Invoke(this, args);
            }
        }

        public IPlaylistCollection Parent { get; private set; }

        public Playlist(IPlaylistCollection parent)
        {
            Parent = parent;

            Songs = new SongCollection(this);

            name = emptyName;
            absolutePath = emptyOrLoadingPath;

            Loop = LoopType.Off;
            Shuffle = ShuffleType.Off;
        }

        public Playlist(string xmlText, IPlaylistCollection parent)
        {
            Parent = parent;
            ReadXml(XmlConverter.GetReader(xmlText));
        }

        public Playlist(IPlaylistCollection parent, string path) : this(parent)
        {
            name = path != string.Empty ? Path.GetFileName(path) : KnownFolders.MusicLibrary.Name;
            absolutePath = path;
        }

        private ILoop GetLooper(LoopType type)
        {
            switch (type)
            {
                case LoopType.All:
                    return LoopAll.Instance;

                case LoopType.Current:
                    return LoopCurrent.Instance;

                default:
                    return LoopOff.Instance;
            }
        }

        public void SetNextSong()
        {
            ChangeCurrentSong(1);
        }

        public void SetPreviousSong()
        {
            ChangeCurrentSong(-1);
        }

        public void ChangeCurrentSong(int offset)
        {
            if (ShuffleSongs.Count == 0) return;

            int shuffleSongsIndex = ShuffleSongs.IndexOf(CurrentSong);
            shuffleSongsIndex = (shuffleSongsIndex + offset + ShuffleSongs.Count) % ShuffleSongs.Count;
            CurrentSong = ShuffleSongs.ElementAt(shuffleSongsIndex);

            if (CurrentSong.Failed)
            {
                if (Songs.All(x => x.Failed)) return;

                ChangeCurrentSong(offset);
            }
        }

        public void SetNextShuffle()
        {
            switch (ShuffleSongs.Type)
            {
                case ShuffleType.Off:
                    Shuffle = ShuffleType.OneTime;
                    break;

                case ShuffleType.OneTime:
                    Shuffle = ShuffleType.Complete;
                    break;

                case ShuffleType.Complete:
                    Shuffle = ShuffleType.Off;
                    break;
            }
        }

        public void SetShuffle(IShuffleCollection shuffleSongs)
        {
            if (shuffleSongs.Parent != this) return;

            var args = new ShuffleChangedEventArgs(ShuffleSongs, shuffleSongs, CurrentSong, CurrentSong);
            ShuffleSongs = shuffleSongs;
            ShuffleChanged?.Invoke(this, args);
        }

        public void SetNextLoop()
        {
            Loop = Looper.GetNext().GetLoopType();
        }

        private IEnumerable<Song> SetCurrentSongsOrderedWithAddedSongs(IEnumerable<Song> currentSongs, IEnumerable<Song> addSongs)
        {
            return GetOrderedSongs(currentSongs.Concat(addSongs));
        }

        private IEnumerable<Song> GetOrderedSongs(IEnumerable<Song> songs)
        {
            return songs.OrderBy(x => x.Title).ThenBy(x => x.Artist);
        }

        private async Task<StorageFolder> GetStorageFolder()
        {
            if (absolutePath == string.Empty) return KnownFolders.MusicLibrary;

            return await StorageFolder.GetFolderFromPathAsync(absolutePath);
        }

        private async Task<IReadOnlyList<StorageFile>> GetStorageFolderFiles()
        {
            try
            {
                StorageFolder folder = await GetStorageFolder();

                return await folder.GetFilesAsync();
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("GetFilesFail", e, Name);
                return new List<StorageFile>();
            }
        }

        public virtual async Task Refresh()
        {
            StorageFile[] files = (await GetStorageFolderFiles()).ToArray();
            IEnumerable<Song> foundSongs = GetSongsFromStorageFiles(files).ToArray();

            if (Parent.Parent.CanceledLoading) return;
       
            Songs.Reset(foundSongs);

            IShuffleCollection newShuffleSongs = new ShuffleOffCollection(this, Songs);
            var args = new ShuffleChangedEventArgs(ShuffleSongs, newShuffleSongs,
                CurrentSong, newShuffleSongs.FirstOrDefault());

            ShuffleSongs = new ShuffleOffCollection(this, Songs);
            CurrentSong = ShuffleSongs.FirstOrDefault();

            ShuffleChanged?.Invoke(this, args);
        }

        private IEnumerable<Song> GetSongsFromStorageFiles(IEnumerable<StorageFile> files)
        {
            return files.AsParallel().Select(f => GetLoadedSong(f)).Where(s => !s.IsEmpty);
        }

        public virtual async Task Update()
        {
            IReadOnlyList<StorageFile> files = await GetStorageFolderFiles();
            Song[] songs = Songs.ToArray();
            IEnumerable<StorageFile> addFiles = files.Where(f => !songs.Any(s => s.Path == f.Path));
            Song[] addSongs = addFiles.Select(f => GetLoadedSong(f)).Where(s => !s.IsEmpty).ToArray();
            Song[] removeSongs = songs.Where(s => !files.Any(f => f.Path == s.Path)).ToArray();

            if (Parent.Parent.CanceledLoading) return;

            if (removeSongs.Length == Songs.Count)
            {
                Parent.Remove(this);
                return;
            }

            Songs.Change(addSongs, removeSongs);
        }

        public virtual async Task AddNew()
        {
            IReadOnlyList<StorageFile> files = await GetStorageFolderFiles();
            Song[] songs = Songs.ToArray();

            IEnumerable<StorageFile> addFiles = files.Where(f => !songs.Any(s => s.Path == f.Path));
            Song[] addSongs = addFiles.Select(f => GetLoadedSong(f)).Where(s => !s.IsEmpty).ToArray();

            if (Parent.Parent.CanceledLoading) return;
            if (addSongs.Length == 0) return;

            Songs.Change(addSongs, null);
        }

        private Song GetLoadedSong(StorageFile file)
        {
            if (Parent.Parent.CanceledLoading) return Song.GetEmpty(Songs);

            try
            {
                return Song.GetLoaded(Songs, file);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("LoadSongFail", e);
            }

            return Song.GetEmpty(Songs);
        }

        public override bool Equals(object obj)
        {
            return this == obj as IPlaylist;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Playlist playlist1, IPlaylist playlist2)
        {
            bool playlist1IsNull = ReferenceEquals(playlist1, null);
            bool playlist2IsNull = ReferenceEquals(playlist2, null);

            if (ReferenceEquals(playlist1, playlist2)) return true;
            if (ReferenceEquals(playlist1, null) || ReferenceEquals(playlist2, null)) return false;
            if (playlist1.AbsolutePath != playlist2.AbsolutePath) return false;
            if (playlist1.CurrentSong != playlist2.CurrentSong) return false;
            if (playlist1.Loop != playlist2.Loop) return false;
            if (playlist1.Name != playlist2.Name) return false;
            if (playlist1.Shuffle != playlist2.Shuffle) return false;
            if (!playlist1.ShuffleSongs.SequenceEqual(playlist2.ShuffleSongs)) return false;
            if (!playlist1.Songs.SequenceEqual(playlist2.Songs)) return false;

            return true;
        }

        public static bool operator !=(Playlist playlist1, IPlaylist playlist2)
        {
            return !playlist1.Equals(playlist2);
        }

        public static bool operator ==(IPlaylist playlist1, Playlist playlist2)
        {
            return playlist2.Equals(playlist1);
        }

        public static bool operator !=(IPlaylist playlist1, Playlist playlist2)
        {
            return !playlist2.Equals(playlist1);
        }

        public override string ToString()
        {
            return Name;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            absolutePath = reader.GetAttribute("AbsolutePath") ?? emptyOrLoadingPath;
            currentSongPositionPercent = double.Parse(reader.GetAttribute("CurrentSongPositionPercent") ?? "0");
            name = reader.GetAttribute("Name") ?? emptyName;
            Loop = (LoopType)Enum.Parse(typeof(LoopType), reader.GetAttribute("Loop") ?? LoopType.Off.ToString());

            string currentSongPath = reader.GetAttribute("CurrentSongPath") ?? string.Empty; ;
            ShuffleType shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType),
                reader.GetAttribute("Shuffle") ?? ShuffleType.Off.ToString());

            reader.ReadStartElement();

            Songs = new SongCollection(this, reader.ReadOuterXml());
            ShuffleSongs = ReadShuffleSongs(shuffle, reader);

            currentSong = Songs.FirstOrDefault(s => s.Path == currentSongPath) ?? Songs.FirstOrDefault();
        }

        private IShuffleCollection ReadShuffleSongs(ShuffleType type, XmlReader reader)
        {
            try
            {
                string outerXml = reader.ReadOuterXml();

                switch (type)
                {
                    case ShuffleType.Off:
                        return new ShuffleOffCollection(this, Songs, outerXml);

                    case ShuffleType.OneTime:
                        return new ShuffleOneTimeCollection(this, Songs, outerXml);

                    case ShuffleType.Complete:
                        return new ShuffleCompleteCollection(this, Songs, outerXml);
                }
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("PlaylistShuffleSongsReadXmlFail", e, AbsolutePath);
            }

            return new ShuffleOffCollection(this, Songs);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("AbsolutePath", AbsolutePath);
            writer.WriteAttributeString("CurrentSongPath", CurrentSong.Path);
            writer.WriteAttributeString("CurrentSongPositionPercent", currentSongPositionPercent.ToString());
            writer.WriteAttributeString("Loop", Loop.ToString());
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Shuffle", Shuffle.ToString());

            writer.WriteStartElement("Songs");
            Songs.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("ShuffleSongs");
            ShuffleSongs.WriteXml(writer);
            writer.WriteEndElement();
        }
    }
}
