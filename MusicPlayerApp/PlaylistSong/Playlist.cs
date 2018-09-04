using PlayerIcons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace PlaylistSong
{
    public enum LoopKind { Off, On, Current };

    public enum ShuffleKind { Off, OneTime, Complete };

    public class Playlist
    {
        private static List<int> emptyShuffleList;
        private static List<Song> emptySongs;

        private List<int> EmptyShuffleList
        {
            get
            {
                if (emptyShuffleList == null) emptyShuffleList = new List<int>() { 0 };

                return emptyShuffleList;
            }
        }

        private List<Song> EmptySongs
        {
            get
            {
                if (emptySongs == null) emptySongs = new List<Song>() { new Song() };

                return emptySongs;
            }
        }

        private const int shuffleCompleteListNextCount = 5, shuffleCompleteListPreviousCount = 3;

        private Random ran;

        private bool loaded = false;
        private int currentSongIndex;
        private double songPostionMilliseconds;
        private string absolutePath;
        private List<int> shuffleList;
        private List<Song> songs;
        private LoopKind loop;
        private ShuffleKind shuffle;

        public Song this[int index]
        {
            get { return Songs[index]; }
            set { Songs[index] = value; }
        }

        public int Lenght { get { return Songs.Count; } }

        public int CurrentSongIndex
        {
            get { return shuffle != ShuffleKind.Complete ? currentSongIndex : GetShuffleCompleteCurrentIndex(); }
            set
            {
                currentSongIndex = GetPossibleSongIndex(value);

                songPostionMilliseconds = 0;
            }
        }

        public string SongCount
        {
            get
            {
                return songs.Count != 1 ? songs.Count.ToString() + " Songs" : "1 Song";
            }
        }

        public List<int> ShuffleList { get { return shuffleList.Count > 0 ? shuffleList : EmptyShuffleList; } }

        public double SongPositionMilliseconds
        {
            get { return songPostionMilliseconds; }
            set { songPostionMilliseconds = value; }
        }

        public string Name { get; private set; }

        public string AbsolutePath { get { return absolutePath; } }

        public string RelativePath { get { return GetRelativePath(absolutePath); } }

        public Song CurrentSong
        {
            get
            {
                return HaveSong(CurrentSongIndex) ? Songs[ShuffleList[CurrentSongIndex]] : new Song();
            }
        }

        private List<Song> Songs { get { return songs.Count > 0 ? songs : EmptySongs; } }

        public LoopKind Loop
        {
            get { return loop; }
            set { loop = value; }
        }

        public static Brush TextBrush
        {
            get
            {
                return Icons.Theme == ElementTheme.Light ? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) :
                    new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }
        }

        public BitmapImage PlayIcon { get { return Library.IsEmpty() ? new BitmapImage() : Icons.PlayImage; } }

        public BitmapImage LoopIcon
        {
            get
            {
                switch (loop)
                {
                    case LoopKind.Off:
                        return Icons.LoopOff;

                    case LoopKind.On:
                        return Icons.LoopOn;

                    case LoopKind.Current:
                        return Icons.LoopCurrent;
                }

                return Icons.LoopOff;
            }
        }

        public ShuffleKind Shuffle
        {
            get { return shuffle; }
            set
            {
                if (shuffle != value)
                {
                    shuffle = value;
                    GenerateShuffleList();
                }
            }
        }

        public BitmapImage ShuffleIcon
        {
            get
            {
                switch (shuffle)
                {
                    case ShuffleKind.Off:
                        return Icons.ShuffleOff;

                    case ShuffleKind.OneTime:
                        return Icons.ShuffleOneTime;

                    case ShuffleKind.Complete:
                        return Icons.ShuffleComplete;
                }

                return Icons.ShuffleOff;
            }
        }

        public Playlist()
        {
            loaded = true;
            Name = "None";

            songs = new List<Song>();
            shuffleList = new List<int>();

            currentSongIndex = 0;
        }

        public Playlist(string path)
        {
            ran = new Random();

            Name = path != "" ? Path.GetFileName(path) : KnownFolders.MusicLibrary.Name;
            absolutePath = path;
            songs = new List<Song>();
            shuffleList = new List<int>();

            CurrentSongIndex = 0;
        }

        public Playlist(SavePlaylist savePlaylist)
        {
            ran = new Random();
            loaded = true;

            Name = savePlaylist.Name;
            songs = new List<Song>();
            absolutePath = savePlaylist.Path;

            foreach (SaveSong saveSong in savePlaylist.Songs)
            {
                songs.Add(new Song(saveSong));
            }

            shuffleList = savePlaylist.ShuffleList.ToList();
            shuffle = savePlaylist.Shuffle;
            loop = savePlaylist.Loop;
            CurrentSongIndex = savePlaylist.CurrentSongIndex;
        }

        private string GetRelativePath(string absolutePath)
        {
            if (absolutePath == "") return "\\Music";
            int index = absolutePath.IndexOf("\\Music");

            return absolutePath.Remove(0, index);
        }

        private bool HaveSong(int index)
        {
            return index >= 0 && index < ShuffleList.Count &&
               ShuffleList[index] >= 0 && ShuffleList[index] < Songs.Count;
        }

        private int GetPossibleSongIndex(int inIndex)
        {
            if (ShuffleList.Count == 0) return -1;

            if (inIndex >= 0 && inIndex < ShuffleList.Count && ShuffleList.Count > 0) return inIndex;

            return inIndex < 0 ? 0 : ShuffleList.Count - 1;
        }

        private async Task<bool> IsStorageFolderEmpty()
        {
            var files = await GetStorageFolderFiles();

            return files.Count == 0;
        }

        public List<Song> GetSongs()
        {
            return Songs;
        }

        public List<Song> GetShuffleSongs()
        {
            List<Song> list = new List<Song>();

            foreach (int i in ShuffleList)
            {
                list.Add(Songs[i]);
            }

            return list;
        }

        public Song GetShuffleSong(int index)
        {
            return HaveSong(index) ? Songs[ShuffleList[index]] : new Song();
        }

        public int GetShuffleListIndex(Song song)
        {
            return ShuffleList.IndexOf(Songs.IndexOf(song));
        }

        public void AddShuffleCompleteSong(bool first, string pathList)
        {
            if (shuffle != ShuffleKind.Complete) return;

            if (first) shuffleList.Reverse();

            string[] array = pathList.TrimEnd(';').Split(';');

            for (int i = 0; i < array.Length; i++)
            {
                shuffleList.Add(GetSongIndexWithPath(array[i]));
                shuffleList.RemoveAt(0);
            }

            if (first) shuffleList.Reverse();
        }

        private int GetSongIndexWithPath(string path)
        {
            return Songs.FindIndex(x => x.Path == path);
        }

        public bool SetNextSong()
        {
            return SetNextSong(1);
        }

        public bool SetNextSong(int count)
        {
            CurrentSongIndex = GetIndexInRange(CurrentSongIndex + count, ShuffleList.Count);

            if (shuffle == ShuffleKind.Complete)
            {
                for (int i = 0; i < count; i++)
                {
                    shuffleList.RemoveAt(0);
                    shuffleList.Add(GetRandomIndexWhichIsNotInShuffleList());
                }

                currentSongIndex = GetShuffleCompleteCurrentIndex();
            }

            return CurrentSongIndex - count < 0 && loop != LoopKind.On;
        }

        public bool SetPreviousSong()
        {
            return SetPreviousSong(1);
        }

        public bool SetPreviousSong(int count)
        {
            CurrentSongIndex = GetIndexInRange(CurrentSongIndex - count, ShuffleList.Count);

            if (shuffle == ShuffleKind.Complete)
            {
                shuffleList.Reverse();

                for (int i = 0; i < count; i++)
                {
                    shuffleList.RemoveAt(0);
                    shuffleList.Add(GetRandomIndexWhichIsNotInShuffleList());
                }

                shuffleList.Reverse();

                currentSongIndex = GetShuffleCompleteCurrentIndex();
            }

            return CurrentSongIndex + count >= ShuffleList.Count && loop == LoopKind.On;
        }

        private int GetIndexInRange(int index ,int length)
        {
            return ((index) % length + length) % length;
        }

        private int GetRandomIndexWhichIsNotInShuffleList()
        {
            int index;

            do
            {
                index = ran.Next(Songs.Count);

            } while (IsIndexInShuffleList(index));

            return index;
        }

        private bool IsIndexInShuffleList(int index)
        {
            return ShuffleList.Contains(index);
        }

        public void SetNextShuffle()
        {
            switch (shuffle)
            {
                case ShuffleKind.Off:
                    Shuffle = ShuffleKind.OneTime;
                    break;

                case ShuffleKind.OneTime:
                    Shuffle = ShuffleKind.Complete;
                    break;

                case ShuffleKind.Complete:
                    Shuffle = ShuffleKind.Off;
                    break;
            }
        }

        public void SetNextLoop()
        {
            switch (loop)
            {
                case LoopKind.Off:
                    loop = LoopKind.On;
                    break;

                case LoopKind.On:
                    loop = LoopKind.Current;
                    break;

                case LoopKind.Current:
                    loop = LoopKind.Off;
                    break;
            }
        }

        public void GenerateShuffleList()
        {
            switch (shuffle)
            {
                case ShuffleKind.Off:
                    SetShuffleListOff();
                    break;

                case ShuffleKind.OneTime:
                    SetShuffleListOneTime();
                    break;

                case ShuffleKind.Complete:
                    SetShuffleListComplete();
                    break;
            }
        }

        private void SetShuffleListOff()
        {
            int lastCurrentIndex = ShuffleList[CurrentSongIndex];
            shuffleList = new List<int>();

            for (int i = 0; i < Songs.Count; i++)
            {
                shuffleList.Add(i);
            }

            CurrentSongIndex = lastCurrentIndex;
        }

        private void SetShuffleListOneTime()
        {
            int lastCurrentIndex = ShuffleList[CurrentSongIndex];
            shuffleList = new List<int>();
            shuffleList.Add(lastCurrentIndex);

            for (int i = 1; i < Songs.Count; i++)
            {
                shuffleList.Add(GetRandomIndexWhichIsNotInShuffleList());
            }

            CurrentSongIndex = 0;
        }

        private void SetShuffleListComplete()
        {
            int lastCurrentIndex = ShuffleList[currentSongIndex];

            shuffleList = new List<int>();
            ChangeShuffleListCount(GetShuffleCompleteCount());

            for (int i = 0; i < shuffleList.Count; i++)
            {
                shuffleList[i] = i != GetShuffleCompleteCurrentIndex() ?
                    GetRandomIndexWhichIsNotInShuffleList() : lastCurrentIndex;
            }

            CurrentSongIndex = GetShuffleCompleteCurrentIndex();
        }

        private int GetShuffleCompleteCount()
        {
            int count = shuffleCompleteListNextCount + shuffleCompleteListPreviousCount + 1;
            return Songs.Count > count ? count : Songs.Count;
        }

        private int GetShuffleCompleteCurrentIndex()
        {
            double indexDouble = (GetShuffleCompleteCount() - 1) / 
                Convert.ToDouble(shuffleCompleteListNextCount + shuffleCompleteListPreviousCount) *
                shuffleCompleteListPreviousCount;

            return Convert.ToInt32(indexDouble);
        }

        public ValueSet GetShuffleAsValueSet(int index)
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add("Index", index.ToString());
            valueSet.Add("Shuffle", shuffle.ToString());

            if (shuffle != ShuffleKind.Off)
            {
                string list = "";

                for (int i = 0; i < shuffleList.Count; i++)
                {
                    list += shuffleList[i].ToString() + ";";
                }

                valueSet.Add("Order", list);
            }

            return valueSet;
        }

        public void SetShuffleList(ValueSet valueSet)
        {
            switch(valueSet["Shuffle"].ToString())
            {
                case "Complete":
                    ChangeShuffleListCount(GetShuffleCompleteCount());
                    CurrentSongIndex = GetShuffleCompleteCurrentIndex();
                    shuffle = ShuffleKind.Complete;
                    break;

                case "Off":
                    ChangeShuffleListCount(Songs.Count);
                    shuffle = ShuffleKind.Off;
                    SetShuffleListOff();
                    return;

                case "OneTime":
                    ChangeShuffleListCount(Songs.Count);
                    CurrentSongIndex = 0;
                    shuffle = ShuffleKind.OneTime;
                    break;
            }

            string listString = valueSet["Order"].ToString().TrimEnd(';');
            string[] list = listString.Split(';');

            for (int i = 0; i < list.Length; i++)
            {
                shuffleList[i] = int.Parse(list[i]);
            }
        }

        private void ChangeShuffleListCount(int count)
        {
            while (shuffleList.Count < count)
            {
                shuffleList.Add(-1);
            }

            shuffleList.RemoveRange(count, shuffleList.Count - count);
        }

        public bool IsEmpty()
        {
            return loaded ? songs.Count == 0 : IsStorageFolderEmpty().Result;
        }

        public void RemoveSong(Song song)
        {
            if (IsEmpty()) return;

            RemoveSong(GetShuffleListIndex(song));
        }

        public void RemoveSong(int shuffleListIndex)
        {
            if (IsEmpty()) return;

            int songsListIndex = shuffleList[shuffleListIndex];

            songs.RemoveAt(songsListIndex);
            shuffleList.RemoveAt(shuffleListIndex);

            if (shuffle != ShuffleKind.Complete) IncreaseEveryIndexOverIndex(shuffleListIndex);
            else ChangeShuffleListBecauseOfReovedSong(shuffleListIndex);
        }

        private void IncreaseEveryIndexOverIndex(int index)
        {
            for (int i = 0; i < shuffleList.Count; i++)
            {
                if (shuffleList[i] > index)
                {
                    shuffleList[i]--;
                }
            }

            if (CurrentSongIndex > index) currentSongIndex--;
            CurrentSongIndex = currentSongIndex;
        }

        private void ChangeShuffleListBecauseOfReovedSong(int index)
        {
            if (shuffleList.Count == songs.Count) return;

            bool first = CurrentSongIndex > index;

            if (first) shuffleList.Reverse();
            shuffleList.Add(GetRandomIndexWhichIsNotInShuffleList());
            if (first) shuffleList.Reverse();
        }

        public async Task<StorageFolder> GetStorageFolder()
        {
            return absolutePath != "" ? await StorageFolder.GetFolderFromPathAsync(absolutePath) : KnownFolders.MusicLibrary;
        }

        private async Task<IReadOnlyList<StorageFile>> GetStorageFolderFiles()
        {
            var folder = await GetStorageFolder();

            return await folder.GetFilesAsync();
        }

        public async Task<bool> LoadSongsFromStorage()
        {
            songs = (await GetSongsFromStorage()).OrderBy(x => x.Title).ToList();

            shuffle = ShuffleKind.Off;
            GenerateShuffleList();

            loaded = true;

            CurrentSongIndex = 0;

            return true;
        }

        private async Task<List<Song>> GetSongsFromStorage()
        {
            List<Song> list = new List<Song>();
            var files = await GetStorageFolderFiles();

            foreach (StorageFile file in files)
            {
                try
                {
                    list.Add(new Song(file.Path));
                }
                catch (Exception e) { }
            }

            return list;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
