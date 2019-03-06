using MusicPlayer.Data.Shuffle;
using MusicPlayer.Data.Simple;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private double currentSongPosition = double.NaN;
        private Song currentSong;
        private ISongCollection songs;
        private LoopType loop = LoopType.Off;

        public event EventHandler<CurrentSongChangedEventArgs> CurrentSongChanged;
        public event EventHandler<CurrentSongPositionChangedEventArgs> CurrentSongPositionChanged;
        public event EventHandler<LoopChangedEventArgs> LoopChanged;
        public event EventHandler<SongsChangedEventArgs> SongsChanged;

        public bool IsEmpty { get { return Songs.Count == 0; } }

        public double CurrentSongPosition
        {
            get
            {
                return !double.IsNaN(currentSongPosition) ? currentSongPosition : Library.DefaultSongsPosition;
            }
            set
            {
                if (value == currentSongPosition) return;
                //MobileDebug.Service.WriteEvent("SetCurrentSongPosition", Name, CurrentSong, currentSongPosition, value);

                var args = new CurrentSongPositionChangedEventArgs(currentSongPosition, value);
                currentSongPosition = value;
                CurrentSongPositionChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(CurrentSongPosition));
            }
        }

        public string Name { get; private set; }

        public string AbsolutePath { get; private set; }

        public Song CurrentSong
        {
            get { return currentSong; }
            set
            {
                if (value == currentSong) return;

                var args = new CurrentSongChangedEventArgs(currentSong, value);
                currentSong = value;
                CurrentSongPosition = 0;
                CurrentSongChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(CurrentSong));
            }
        }

        public ISongCollection Songs
        {
            get { return songs; }
            set
            {
                if (value == songs) return;

                var args = new SongsChangedEventArgs(songs, value);
                songs?.Shuffle?.Dispose();
                songs = value;
                songs.Parent = this;
                SongsChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(Songs));
            }
        }

        public LoopType Loop
        {
            get { return loop; }
            set
            {
                if (value == loop) return;

                var args = new LoopChangedEventArgs(loop, value);
                loop = value;
                LoopChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(Loop));
            }
        }

        public IPlaylistCollection Parent { get; set; }

        public Playlist()
        {
            Songs = new SongCollection();

            Name = emptyName;
            AbsolutePath = emptyOrLoadingPath;

            Loop = LoopType.Off;
        }

        public Playlist(string path) : this()
        {
            Name = path != string.Empty ? Path.GetFileName(path) : KnownFolders.MusicLibrary.Name;
            AbsolutePath = path;
        }

        public Playlist(CurrentPlaySong currentPlaySong) : this()
        {
            Songs.Add(new Song(currentPlaySong));
            CurrentSong = Songs.FirstOrDefault();
            CurrentSongPosition = currentPlaySong.Position;
        }

        private async Task<StorageFolder> GetStorageFolder()
        {
            if (AbsolutePath == string.Empty) return KnownFolders.MusicLibrary;

            return await StorageFolder.GetFolderFromPathAsync(AbsolutePath);
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

        public async Task Reset(StopOperationToken stopToken)
        {
            IReadOnlyList<StorageFile> files = await GetStorageFolderFiles();
            Song[] foundSongs = (await GetSongsFromStoragefiles(files, stopToken)).ToArray();

            if (stopToken.IsStopped) return;

            Songs = new SongCollection(foundSongs, ShuffleType.Off, null);
            CurrentSong = Songs.Shuffle.FirstOrDefault();
        }

        public async Task ResetSongs(StopOperationToken stopToken)
        {
            await Task.WhenAll(Songs.Select(s => s.Reset()).ToArray());
        }

        public async Task Update(StopOperationToken stopToken)
        {
            Song[] songs = Songs.ToArray();
            IReadOnlyList<StorageFile> files = await GetStorageFolderFiles();
            IEnumerable<StorageFile> addFiles = files.Where(f => !songs.Any(s => s.Path == f.Path));
            Song[] addSongs = (await GetSongsFromStoragefiles(addFiles, stopToken)).ToArray();
            Song[] removeSongs = songs.Where(s => !files.Any(f => f.Path == s.Path)).ToArray();

            if (stopToken.IsStopped) return;

            if (Songs.Count + addSongs.Length - removeSongs.Length == 0)
            {
                Parent.Remove(this);
                return;
            }

            Songs.Change(removeSongs, addSongs);
        }

        public async Task AddNew(StopOperationToken stopToken)
        {
            IReadOnlyList<StorageFile> files = await GetStorageFolderFiles();
            Song[] songs = Songs.ToArray();

            IEnumerable<StorageFile> addFiles = files.Where(f => !songs.Any(s => s.Path == f.Path));
            Song[] addSongs = (await GetSongsFromStoragefiles(addFiles, stopToken)).ToArray();

            if (stopToken.IsStopped) return;
            if (addSongs.Length == 0) return;

            Songs.Change(null, addSongs);
        }

        private async Task<IEnumerable<Song>> GetSongsFromStoragefiles(IEnumerable<StorageFile> files, StopOperationToken stopToken)
        {
            Task<Song>[] tasks = files.Select(f => GetLoadedSong(f, stopToken)).ToArray();

            await Task.WhenAll(tasks);

            return tasks.Select(t => t.Result).Where(s => s != null && !s.IsEmpty);
        }

        private async Task<Song> GetLoadedSong(StorageFile file, StopOperationToken stopToken)
        {
            if (stopToken.IsStopped) return null;

            try
            {
                return await Song.GetLoaded(file);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("LoadSongFail", e);
            }

            return null;
        }

        public IPlaylist ToSimple()
        {
            Playlist playlist = new Playlist();
            playlist.AbsolutePath = AbsolutePath;
            playlist.CurrentSong = CurrentSong;
            playlist.CurrentSongPosition = CurrentSongPosition;
            playlist.Loop = Loop;
            playlist.Name = Name;
            playlist.Songs = Songs.ToSimple();

            return playlist;
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

            if (!(obj is IPlaylist)) return false;

            IPlaylist other = (IPlaylist)obj;

            if (AbsolutePath != other.AbsolutePath) return false;
            if (CurrentSong != other.CurrentSong) return false;
            if (Loop != other.Loop) return false;
            if (Name != other.Name) return false;
            if (Songs.Shuffle?.Type != other.Songs.Shuffle?.Type) return false;
            if (!Songs.SequenceEqual(other.Songs)) return false;
            if (!Songs.Shuffle.SequenceEqual(other.Songs.Shuffle)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Playlist playlist1, IPlaylist playlist2)
        {
            return (playlist1?.Equals(playlist2) ?? playlist2?.Equals(playlist1)) ?? true;
        }

        public static bool operator !=(Playlist playlist1, IPlaylist playlist2)
        {
            return !(playlist1 == playlist2);
        }

        public static bool operator ==(IPlaylist playlist1, Playlist playlist2)
        {
            return playlist2 == playlist1;
        }

        public static bool operator !=(IPlaylist playlist1, Playlist playlist2)
        {
            return !(playlist1 == playlist2);
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
            double currentSongPosition = double.Parse(reader.GetAttribute("CurrentSongPosition") ?? "0");

            AbsolutePath = reader.GetAttribute("AbsolutePath") ?? emptyOrLoadingPath;
            Name = reader.GetAttribute("Name") ?? emptyName;
            Loop = (LoopType)Enum.Parse(typeof(LoopType), reader.GetAttribute("Loop") ?? LoopType.Off.ToString());

            string currentSongPath = reader.GetAttribute("CurrentSongPath") ?? string.Empty; ;
            ShuffleType shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType),
                reader.GetAttribute("Shuffle") ?? ShuffleType.Off.ToString());

            reader.ReadStartElement();

            ISongCollection songs = reader.Name == typeof(SongCollection).Name ?
                (ISongCollection)new SongCollection() : new SimpleSongCollection();

            songs.Parent = this;
            Songs = XmlConverter.Deserialize(songs, reader.ReadOuterXml());
            
            CurrentSong = songs.FirstOrDefault(s => s.Path == currentSongPath) ?? songs.FirstOrDefault();
            CurrentSongPosition = currentSongPosition;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("AbsolutePath", AbsolutePath);
            writer.WriteAttributeString("CurrentSongPath", CurrentSong.Path);
            writer.WriteAttributeString("CurrentSongPosition", currentSongPosition.ToString());
            writer.WriteAttributeString("Loop", Loop.ToString());
            writer.WriteAttributeString("Name", Name);

            writer.WriteStartElement(Songs.GetType().Name);
            Songs.WriteXml(writer);
            writer.WriteEndElement();
        }
    }
}
