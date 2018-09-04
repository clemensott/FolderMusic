using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace MusicPlayer.Data
{
    public class Playlist
    {
        public const double DefaultSongsPositionPercent = 0, DefaultSongsPositionMillis = 1;
        private const string emptyName = "None", emptyOrLoadingPath = "None";

        protected int songsIndex = 0;
        protected double songPositionPercent = double.NaN;
        protected string name = emptyName, absolutePath = emptyOrLoadingPath;
        protected LoopType loop = LoopType.Off;
        protected ShuffleType shuffle = ShuffleType.Empty;
        protected List<int> shuffleList = new List<int>();
        protected SongList songs = new SongList();

        [XmlIgnore]
        public Song this[int index]
        {
            get { return Songs[index]; }
            set { Songs[index] = value; }
        }

        [XmlIgnore]
        public bool IsEmptyOrLoading { get { return GetIsEmptyOrLoading(); } }

        [XmlIgnore]
        public int PlaylistIndex { get { return GetPlaylistIndex(); } }

        public int SongsIndex
        {
            get { return GetSongsIndex(); }
            set { SetSongIndex(value); }
        }

        [XmlIgnore]
        public int ShuffleListIndex
        {
            get { return GetShuffleListIndex(); }
            set { SetShuffleListIndex(value); }
        }

        [XmlIgnore]
        public string SongCount { get { return Songs.Count != 1 ? Songs.Count.ToString() + " Songs" : "1 Song"; } }

        public double SongPositionPercent
        {
            get
            {
                return !double.IsNaN(songPositionPercent) ? songPositionPercent : DefaultSongsPositionPercent;
            }
            set
            {
                if (value == songPositionPercent) return;
                else if (IsEmptyOrLoading) songPositionPercent = value;
                else SetSongs(Songs, Shuffle, ShuffleList, CurrentSong, value);
            }
        }

        public double SongPositionMillis
        {
            get
            {
                return SongPositionPercent != DefaultSongsPositionPercent ?
                    SongPositionPercent * CurrentSong.NaturalDurationMilliseconds : DefaultSongsPositionMillis;
            }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string AbsolutePath
        {
            get { return absolutePath; }
            set { absolutePath = value; }
        }

        public List<int> ShuffleList
        {
            get { return shuffleList; }
            set
            {
                if (value == shuffleList) return;
                else if (IsEmptyOrLoading) shuffleList = value;
                else SetSongs(Songs, Shuffle, value, CurrentSong, SongPositionPercent);
            }
        }

        [XmlIgnore]
        public string RelativePath { get { return GetRelativePath(absolutePath); } }

        [XmlIgnore]
        public Song CurrentSong { get { return Songs.ElementAtOrDefault(SongsIndex); } }

        public SongList Songs
        {
            get { return GetSongs(); }
            set { SetSongs(value); }
        }

        [XmlIgnore]
        public IEnumerable<Song> ShuffleSongs
        {
            get
            {
                if (!IsEmptyOrLoading) Shuffler.CheckShuffleList(ref shuffleList, Songs.Count);

                return ShuffleList.Select(i => this[i]);
            }
        }

        public LoopType Loop
        {
            get { return loop; }
            set { SetLoop(value); }
        }

        internal ILoop Looper { get { return GetLooper(Loop); } }

        public ShuffleType Shuffle
        {
            get { return shuffle; }
            set { SetShuffle(value); }
        }

        internal IShuffle Shuffler { get { return GetShuffler(Shuffle); } }

        public Playlist()
        {
            //Name = Library.IsLoaded() ? "None" : "Loading";

            Loop = LoopType.Off;
            Shuffle = ShuffleType.Off;
        }

        public Playlist(string path)
        {
            Name = path != string.Empty ? Path.GetFileName(path) : KnownFolders.MusicLibrary.Name;
            absolutePath = path;
        }

        internal static string GetRelativePath(string absolutePath)
        {
            if (absolutePath == string.Empty) return "\\Music";
            int index = absolutePath.IndexOf("\\Music");

            return absolutePath.Remove(0, index);
        }

        private bool IsShuffleListIndex(int index)
        {
            return ShuffleList.Count > index && index >= 0;
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

        private IShuffle GetShuffler(ShuffleType type)
        {
            switch (type)
            {
                case ShuffleType.Complete:
                    return ShuffleComplete.Instance;

                case ShuffleType.OneTime:
                    return ShuffleOneTime.Instance;

                default:
                    return ShuffleOff.Instance;
            }
        }

        internal void UpdateAddSong(int index, Song addSong, IList<Song> oldSongs, Song currentSong)
        {
            int newSongIndex = Songs.IndexOf(currentSong);

            if (newSongIndex != -1) songsIndex = newSongIndex;
            else SongPositionPercent = 0;

            List<int> oldShuffleList = ShuffleList;
            Shuffler.AddSongsToShuffleList(ref shuffleList, oldSongs, Songs);

            Feedback.Current.RaiseSongsPropertyChanged(this, new ChangedSong[] { new ChangedSong(index, addSong) },
                new ChangedSong[0], Shuffle, Shuffle, oldShuffleList, ShuffleList, currentSong, CurrentSong);
        }

        internal void UpdateRemoveSong(int index, Song removeSong, Song currentSong)
        {
            int newSongIndex = Songs.IndexOf(currentSong);

            if (newSongIndex != -1) songsIndex = newSongIndex;
            else SongPositionPercent = 0;

            List<int> oldShuffleList = ShuffleList;
            Shuffler.RemoveSongsIndex(index, ref shuffleList, Songs.Count);

            Feedback.Current.RaiseSongsPropertyChanged(this, new ChangedSong[0],
                new ChangedSong[] { new ChangedSong(index, removeSong) },
                Shuffle, Shuffle, oldShuffleList, ShuffleList, currentSong, CurrentSong);
        }

        internal void UpdateAddRemoveSong(int index, Song addSong, Song removeSong, Song currentSong)
        {
            int newSongIndex = Songs.IndexOf(currentSong);

            if (newSongIndex != -1) songsIndex = newSongIndex;
            else SongPositionPercent = 0;

            List<int> oldShuffleList = ShuffleList;
            Shuffler.RemoveSongsIndex(index, ref shuffleList, Songs.Count);

            Feedback.Current.RaiseSongsPropertyChanged(this, new ChangedSong[] { new ChangedSong(index, addSong) },
                new ChangedSong[] { new ChangedSong(index, removeSong) },
                Shuffle, Shuffle, oldShuffleList, ShuffleList, currentSong, CurrentSong);
        }

        internal void SetSongs(SongList songs, ShuffleType shuffle,
            List<int> shuffleList, Song currentSong, double songPositionPercent)
        {
            bool songsChanged = false, shuffleChanged = false,
                currentSongChanged = false, songPositionPercentChanged = false;
            int songsIndex = songs.IndexOf(currentSong);
            IList<Song> oldSongs = Songs;
            ShuffleType oldShuffleType = Shuffle;
            List<int> oldShuffleList = ShuffleList;
            Song oldCurrentSong = CurrentSong;
            double oldSongPositionPercent = SongPositionPercent;

            if (!songs.SequenceEqual(Songs)) songsChanged = true;
            if (songs != oldSongs) this.songs = songs;

            if (shuffle != oldShuffleType)
            {
                this.shuffle = shuffle;
                shuffleChanged = true;
            }

            if (!shuffleList.SequenceEqual(ShuffleList)) shuffleChanged = true;
            if (shuffleList != ShuffleList) this.shuffleList = shuffleList;

            if (currentSong != oldCurrentSong) currentSongChanged = true;
            if (songsIndex != this.songsIndex) this.songsIndex = songsIndex;

            if (songPositionPercent != oldSongPositionPercent)
            {
                this.songPositionPercent = songPositionPercent;
                songPositionPercentChanged = true;
            }

            if (IsEmptyOrLoading) return;

            if (songsChanged)
            {
                Feedback.Current.RaiseSongsPropertyChanged(this, oldSongs, Songs,
                    oldShuffleType, Shuffle, oldShuffleList, ShuffleList, oldCurrentSong, CurrentSong);
            }
            else if (shuffleChanged)
            {
                Feedback.Current.RaiseShufflePropertyChanged(this, oldShuffleType, Shuffle,
                    oldShuffleList, ShuffleList, oldCurrentSong, CurrentSong);
            }
            else if (currentSongChanged)
            {
                Feedback.Current.RaiseCurrentSongPropertyChanged(this, oldCurrentSong, CurrentSong);
            }
            else if (songPositionPercentChanged)
            {
                Feedback.Current.RaiseCurrentSongPositionPropertyChanged(this, oldSongPositionPercent, songPositionPercent);
            }
        }

        public bool SetNextSong()
        {
            return ChangeCurrentSong(1);
        }

        public bool SetPreviousSong()
        {
            return ChangeCurrentSong(-1);
        }

        public virtual bool ChangeCurrentSong(int offset)
        {
            if (ShuffleList.Count == 0) return true;
            ShuffleListIndex = GetIndexInRange(ShuffleListIndex + offset, ShuffleList.Count);

            if (CurrentSong.Failed)
            {
                if (Songs.All(x => x.Failed)) return true;

                return ChangeCurrentSong(offset);
            }

            return Loop != LoopType.All && !IsShuffleListIndex(ShuffleListIndex - offset);
        }

        private int GetIndexInRange(int index, int length)
        {
            return ((index) % length + length) % length;
        }

        public void SetNextShuffle()
        {
            Shuffle = Shuffler.GetNext().GetShuffleType();
        }

        public void SetNextLoop()
        {
            Loop = Looper.GetNext().GetLoopType();
        }

        private List<Song> SetCurrentSongsOrderedWithAddedSongs(IList<Song> currentSongs, IList<Song> addSongs)
        {
            List<Song> updatedSongs = new List<Song>(currentSongs);
            updatedSongs.AddRange(addSongs);

            return GetOrderedSongs(updatedSongs);
        }

        private List<Song> GetOrderedSongs(List<Song> songs)
        {
            return songs.OrderBy(x => x.Title).ThenBy(x => x.Artist).ToList();
        }

        private async Task<StorageFolder> GetStorageFolder()
        {
            if (absolutePath == string.Empty) return KnownFolders.MusicLibrary;

            return await StorageFolder.GetFolderFromPathAsync(absolutePath);
        }

        private async Task<List<StorageFile>> GetStorageFolderFiles()
        {
            try
            {
                StorageFolder folder = await GetStorageFolder();

                return (await folder.GetFilesAsync()).ToList();
            }
            catch
            {
                return new List<StorageFile>();
            }
        }

        public virtual async Task LoadSongsFromStorage()
        {
            List<StorageFile> files = await GetStorageFolderFiles();
            List<Song> foundSongs = await GetSongsFromStorageFiles(files);
            List<Song> updatedSongs = SetCurrentSongsOrderedWithAddedSongs(new List<Song>(), foundSongs);
            int songsIndex = 0;
            ShuffleType shuffle = ShuffleType.Off;
            List<int> shuffleList = GetShuffler(shuffle).GenerateShuffleList(songsIndex, updatedSongs.Count);

            if (Library.Current.CanceledLoading) return;

            SetSongs(new SongList(updatedSongs), shuffle, shuffleList, updatedSongs.ElementAtOrDefault(songsIndex), 0);
        }

        private async Task<List<Song>> GetSongsFromStorageFiles(List<StorageFile> files)
        {
            Task addTask;
            Song addSong;
            List<Task> refreshSongs = new List<Task>();
            List<Song> list = new List<Song>();

            foreach (StorageFile file in files)
            {
                if (Library.Current.CanceledLoading) return list;

                addSong = new Song(file.Path);
                addTask = new Task(addSong.Refresh);

                addTask.Start();
                refreshSongs.Add(addTask);

                list.Add(addSong);
            }

            foreach (Task task in refreshSongs) await task;

            return list;
        }

        public virtual async Task UpdateSongsFromStorage()
        {
            List<Task> refreshSongs = new List<Task>();
            List<string> songsPath = Songs.Select(x => x.Path).ToList();
            List<StorageFile> files = await GetStorageFolderFiles();
            List<Song> updatedSongs = new List<Song>();

            foreach (StorageFile file in files)
            {
                if (Library.Current.CanceledLoading) return;

                if (songsPath.Contains(file.Path)) updatedSongs.Add(Songs[songsPath.IndexOf(file.Path)]);
                else
                {
                    Song addSong = new Song(file.Path);
                    Task addTask = new Task(addSong.Refresh);

                    addTask.Start();
                    refreshSongs.Add(addTask);

                    updatedSongs.Add(addSong);
                }
            }

            foreach (Task task in refreshSongs) await task;

            if (updatedSongs.Count == 0)
            {
                Library.Current.Playlists.Remove(this);
                return;
            }

            List<int> shuffleList = ShuffleList;
            Song currentSong = updatedSongs.Contains(CurrentSong) ? CurrentSong : updatedSongs.First();
            updatedSongs = GetOrderedSongs(updatedSongs);
            Shuffler.AddSongsToShuffleList(ref shuffleList, Songs, updatedSongs);

            SetSongs(new SongList(updatedSongs), Shuffle, shuffleList, currentSong, SongPositionPercent);
        }

        public virtual async Task SearchForNewSongs()
        {
            var files = await GetStorageFolderFiles();
            List<StorageFile> addFiles = GetStorageFilesWhichAreNotInSongs(files);

            if (addFiles.Count == 0) return;

            SetSongsAndShuffleListWithAddSongs(await GetSongsFromStorageFiles(addFiles));
        }

        private List<StorageFile> GetStorageFilesWhichAreNotInSongs(IReadOnlyList<StorageFile> allFiles)
        {
            List<string> songsPaths = songs.Select(x => x.Path).ToList();
            List<StorageFile> addFiles = new List<StorageFile>();

            foreach (StorageFile file in allFiles)
            {
                if (!songsPaths.Contains(file.Path)) addFiles.Add(file);
            }

            return addFiles;
        }

        private void SetSongsAndShuffleListWithAddSongs(List<Song> addSongs)
        {
            List<Song> updatedSongs = SetCurrentSongsOrderedWithAddedSongs(songs, addSongs);
            List<int> shuffleList = ShuffleList;

            Shuffler.AddSongsToShuffleList(ref shuffleList, Songs, updatedSongs);

            SetSongs(new SongList(updatedSongs), Shuffle, shuffleList, CurrentSong, SongPositionPercent);
        }

        public override bool Equals(object obj)
        {
            return this == obj as Playlist;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Playlist playlist1, Playlist playlist2)
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
            if (!playlist1.ShuffleList.SequenceEqual(playlist2.ShuffleList)) return false;
            if (playlist1.ShuffleListIndex != playlist2.ShuffleListIndex) return false;
            //if (playlist1.SongPositionPercent != playlist2.SongPositionPercent) return false;
            if (!playlist1.Songs.SequenceEqual(playlist2.Songs)) return false;
            if (playlist1.SongsIndex != playlist2.SongsIndex) return false;

            return true;
        }

        public static bool operator !=(Playlist playlist1, Playlist playlist2)
        {
            return !(playlist1 == playlist2);
        }

        public override string ToString()
        {
            return Name;
        }

        protected virtual bool GetIsEmptyOrLoading()
        {
            return !Library.IsLoaded(this) || songs.Count == 0;
        }

        protected virtual int GetPlaylistIndex()
        {
            return Library.Current.GetPlaylistIndex(this);
        }

        protected virtual int GetSongsIndex()
        {
            return !IsEmptyOrLoading || (songsIndex >= 0 && songsIndex < songs.Count) ? songsIndex : 0;
        }

        protected virtual void SetSongIndex(int newSongIndex)
        {
            if (songsIndex == newSongIndex || newSongIndex == -1) return;
            else if (IsEmptyOrLoading) songsIndex = newSongIndex;
            else
            {
                List<int> shuffleList = ShuffleList;
                Shuffler.GetChangedShuffleListBecauseOfOtherSongsIndex(newSongIndex, ref shuffleList, Songs.Count);

                SetSongs(Songs, Shuffle, shuffleList, Songs[newSongIndex], 0);
            }
        }

        protected virtual int GetShuffleListIndex()
        {
            return Shuffler.GetShuffleListIndex(SongsIndex, ShuffleList, Songs.Count);
        }

        protected virtual void SetShuffleListIndex(int value)
        {
            if (!IsShuffleListIndex(value)) return;

            SongsIndex = ShuffleList[value];
        }

        protected virtual SongList GetSongs()
        {
            return songs;
        }

        protected virtual void SetSongs(SongList newSongs)
        {
            if (newSongs == songs) return;

            if (IsEmptyOrLoading || newSongs.SequenceEqual(songs)) songs = newSongs;
            else
            {
                List<int> shuffleList = Shuffler.GenerateShuffleList(SongsIndex, newSongs.Count);

                SetSongs(newSongs, Shuffle, shuffleList, CurrentSong, SongPositionPercent);
            }
        }
        protected virtual void SetLoop(LoopType value)
        {
            if (Loop == value) return;

            LoopType oldType = loop;
            loop = value;

            Feedback.Current.RaiseLoopPropertyChanged(this, oldType, loop);
        }

        protected virtual void SetShuffle(ShuffleType value)
        {
            if (value == shuffle) return;
            if (IsEmptyOrLoading) shuffle = value;
            else
            {
                List<int> shuffleList = GetShuffler(value).GenerateShuffleList(SongsIndex, Songs.Count);

                SetSongs(Songs, value, shuffleList, CurrentSong, SongPositionPercent);
            }
        }

    }
}
