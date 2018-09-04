using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using System.Xml;
using System.Xml.Schema;

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
                MobileDebug.Manager.WriteEvent("CurrentSongSet", Name);
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

        public Playlist(IPlaylistCollection parent, XmlReader reader)
        {
            Parent = parent;
            ReadXml(reader);
        }

        public Playlist(string xmlText, IPlaylistCollection parent)
            : this(parent, XmlConverter.GetReader(xmlText))
        {
        }

        public Playlist(IPlaylistCollection parent, string path) : this(parent)
        {
            name = path != string.Empty ? Path.GetFileName(path) : KnownFolders.MusicLibrary.Name;
            absolutePath = path;
        }

        public static string GetRelativePath(string absolutePath)
        {
            if (absolutePath == string.Empty) return "\\Music";
            int index = absolutePath.IndexOf("\\Music");

            return absolutePath.Remove(0, index);
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
            MobileDebug.Manager.WriteEvent("SetShuffleSongs1", shuffleSongs.Parent == this);
            if (shuffleSongs.Parent != this) return;

            var args = new ShuffleChangedEventArgs(ShuffleSongs, shuffleSongs, CurrentSong, CurrentSong);
            ShuffleSongs = shuffleSongs;
            MobileDebug.Manager.WriteEvent("SetShuffleSongs2");
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
                MobileDebug.Manager.WriteEvent("GetFilesFail", e, Name);
                return new List<StorageFile>();
            }
        }

        public virtual async Task Refresh()
        {
            StorageFile[] files = (await GetStorageFolderFiles()).ToArray();
            IEnumerable<Song> foundSongs = GetSongsFromStorageFiles(files).ToArray();

            if (Parent.Parent.CanceledLoading) return;

            Songs.Reset(foundSongs);
            ShuffleSongs = new ShuffleOffCollection(this, Songs);
            CurrentSong = ShuffleSongs.FirstOrDefault();
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
                MobileDebug.Manager.WriteEvent("LoadSongFail", e);
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
            absolutePath = reader.GetAttribute("AbsolutePath");
            string currentSongPath = reader.GetAttribute("CurrentSongPath");
            currentSongPositionPercent = double.Parse(reader.GetAttribute("CurrentSongPositionPercent"));
            name = reader.GetAttribute("Name");
            try
            {
                Loop = (LoopType)Enum.Parse(typeof(LoopType), reader.GetAttribute("Loop"));
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistWriteXmlLoopFail", e, Name, reader.GetAttribute("Loop"));
            }
            ShuffleType shuffle = ShuffleType.Off;
            try
            {
                shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType), reader.GetAttribute("Shuffle"));
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistReadXmlShuffleFail", e, Name, reader.GetAttribute("Shuffle"));
            }

            reader.ReadStartElement();
            try
            {
                Songs = new SongCollection(this, reader);
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistReadXmlShuffleFail", e, Name, reader.GetAttribute("Shuffle"));
            }
            reader.ReadEndElement();

            ShuffleSongs = ReadShuffleSongs(shuffle, reader);

            currentSong = Songs.Any(s => s.Path == currentSongPath) ? Songs.First(s => s.Path == currentSongPath) : Songs.FirstOrDefault();
        }

        private IShuffleCollection ReadShuffleSongs(ShuffleType type, XmlReader reader)
        {
            ShuffleCollectionBase collection = null;
            reader.ReadStartElement();

            switch (type)
            {
                case ShuffleType.Off:
                    collection = new ShuffleOffCollection(this, Songs, reader);
                    break;

                case ShuffleType.OneTime:
                    collection = new ShuffleOneTimeCollection(this, Songs, reader);
                    break;

                case ShuffleType.Complete:
                    collection = new ShuffleCompleteCollection(this, Songs, reader);
                    break;
            }

            reader.ReadEndElement();

            return collection;
        }

        public void WriteXml(XmlWriter writer)
        {
            try
            {
                writer.WriteAttributeString("AbsolutePath", AbsolutePath);
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistWriteXmlPathFail", e, Name);
            }
            try
            {
                writer.WriteAttributeString("CurrentSongPath", CurrentSong.Path);
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistWriteXmlCurrentSongFail", e, Name, CurrentSong == null);
            }
            try
            {
                writer.WriteAttributeString("CurrentSongPositionPercent", currentSongPositionPercent.ToString());
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistWriteXmlPositionFail", e, Name);
            }
            try
            {
                writer.WriteAttributeString("Loop", Loop.ToString());
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistWriteXmlLoopFail", e, Name);
            }
            try
            {
                writer.WriteAttributeString("Name", Name);
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistWriteXmlNameFail", e, Name);
            }
            try
            {
                writer.WriteAttributeString("Shuffle", Shuffle.ToString());
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistWriteXmlShuffleFail", e, Name);
            }


            writer.WriteStartElement("Songs");
            try
            {
                Songs.WriteXml(writer);
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistWriteXmlSongsFail", e, Name);
            }

            writer.WriteEndElement();

            writer.WriteStartElement("ShuffleSongs");
            try
            {
                ShuffleSongs.WriteXml(writer);
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("PlaylistWriteXmlShuffleSongsFail", e, Name);
            }
            writer.WriteEndElement();
        }
    }
}
