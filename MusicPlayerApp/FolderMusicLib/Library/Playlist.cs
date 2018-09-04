using FolderMusicLib;
using PlayerIcons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace LibraryLib
{
    public enum LoopKind { Off, On, Current };

    public enum ShuffleKind { Off, OneTime, Complete };

    public class Playlist : INotifyPropertyChanged
    {
        private const int shuffleCompleteListNextCount = 5, shuffleCompleteListPreviousCount = 3;
        private const string emptyOrLoadingPath = "None";

        private Random ran = new Random();

        private int songsIndex = 0;
        private double songPostionMilliseconds;
        private string name, absolutePath;
        private LoopKind loop;
        private ShuffleKind shuffle;
        private List<int> shuffleList;
        private List<Song> songs;

        private bool scrollDefaultAndShuffleSongsLbx;
        private ListBox defaultSongsLbx, shuffleSongsLbx;

        [XmlIgnore]
        public Song this[int index]
        {
            get { return Songs[index]; }
            set { Songs[index] = value; }
        }

        [XmlIgnore]
        public bool IsEmptyOrLoading { get { return songs.Count == 0; } }

        [XmlIgnore]
        public int Length { get { return Songs.Count; } }

        public int PlaylistIndex { get { return Library.Current.GetPlaylistIndex(this); } }

        public int SongsIndex
        {
            get { return GetPossibleSongsIndex(songsIndex); }
            set
            {
                DoScrollLbx();
                if (songsIndex == value) return;

                if (!IsEmptyOrLoading)
                {
                    ChangeShuffleListBecauseOfChangedSongIndex(value);
                    songPostionMilliseconds = 0;
                }

                songsIndex = value;

                if (Library.Current.IsForeground)
                {
                    if (Library.IsLoaded) BackgroundCommunicator.SendPlaylistsAndSongsIndexAndShuffleIfComplete(this);

                    UpdateCurrentSong();
                }
                else { }
            }
        }

        [XmlIgnore]
        public int ShuffleListIndex
        {
            get { return Shuffle != ShuffleKind.Complete ? ShuffleList.IndexOf(songsIndex) :
                    GetShuffleCompleteCurrentIndex(Songs.Count); }
            set
            {
                if (!IsShuffleListIndex(value)) return;

                SongsIndex = ShuffleList[value];
            }
        }

        [XmlIgnore]
        public string SongCount
        {
            get
            {
                return songs.Count != 1 ? songs.Count.ToString() + " Songs" : "1 Song";
            }
        }

        public double SongPositionMilliseconds
        {
            get { return songPostionMilliseconds; }
            set { songPostionMilliseconds = value; }
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
            set { shuffleList = value; }
        }

        [XmlIgnore]
        public string RelativePath { get { return GetRelativePath(absolutePath); } }

        [XmlIgnore]
        public static Brush TextBrush
        {
            get
            {
                return Icons.Theme == ElementTheme.Light ? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) :
                    new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }
        }

        [XmlIgnore]
        public BitmapImage LoopIcon
        {
            get
            {
                try
                {
                    switch (Loop)
                    {
                        case LoopKind.Off:
                            return Icons.LoopOff;

                        case LoopKind.On:
                            return Icons.LoopOn;

                        case LoopKind.Current:
                            return Icons.LoopCurrent;
                    }
                }
                catch { }

                return new BitmapImage();
            }
        }

        [XmlIgnore]
        public BitmapImage ShuffleIcon
        {
            get
            {
                try
                {
                    switch (Shuffle)
                    {
                        case ShuffleKind.Off:
                            return Icons.ShuffleOff;

                        case ShuffleKind.OneTime:
                            return Icons.ShuffleOneTime;

                        case ShuffleKind.Complete:
                            return Icons.ShuffleComplete;
                    }
                }
                catch { }

                return new BitmapImage();
            }
        }

        [XmlIgnore]
        public BitmapImage PlayIcon { get { return Library.Current.IsEmpty ? new BitmapImage() : Icons.PlayImage; } }

        [XmlIgnore]
        public Song CurrentSong { get { return !IsEmptyOrLoading ? Songs[SongsIndex] : new Song(); } }

        public List<Song> Songs
        {
            get { return songs; }
        }

        public List<Song> ShuffleSongs
        {
            get
            {
                if (IsEmptyOrLoading) return new List<Song>() { new Song() };

                List<Song> list = new List<Song>();

                foreach (int i in ShuffleList)
                {
                    list.Add(Songs[i]);
                }

                return list;
            }
        }

        public LoopKind Loop
        {
            get { return loop; }
            set
            {
                if (loop == value) return;

                loop = value;
                UpdateLoopIcon();
            }
        }

        public ShuffleKind Shuffle
        {
            get { return shuffle; }
            set
            {
                if (shuffle == value) return;

                shuffle = value;
                UpdateShuffleIcon();
            }
        }

        public Playlist()
        {
            Name = Library.IsLoaded ? "None" : "Loading";
            absolutePath = emptyOrLoadingPath;

            Loop = LoopKind.Off;
            Shuffle = ShuffleKind.Off;

            songs = new List<Song>();
            shuffleList = new List<int>();
        }

        public Playlist(string path)
        {
            Name = path != "" ? Path.GetFileName(path) : KnownFolders.MusicLibrary.Name;
            absolutePath = path;

            songs = new List<Song>();
            shuffleList = new List<int>();
        }

        public Playlist(Song currentSong, double currentSongPositionMilliseconds)
        {
            Name = Library.IsLoaded ? "None" : "Loading";
            absolutePath = emptyOrLoadingPath;

            Loop = LoopKind.Off;
            Shuffle = ShuffleKind.Off;

            songs = new List<Song>() { currentSong };
            shuffleList = new List<int>() { 0 };

            songsIndex = 0;
            songPostionMilliseconds = currentSongPositionMilliseconds;
        }

        public void SetDefaultSongsLbx(ListBox lbx)
        {
            defaultSongsLbx = lbx;
            scrollDefaultAndShuffleSongsLbx = true;
        }

        public void SetShuffleSongsLbx(ListBox lbx)
        {
            shuffleSongsLbx = lbx;
            scrollDefaultAndShuffleSongsLbx = true;
        }

        public void DoScrollLbx()
        {
            if (!scrollDefaultAndShuffleSongsLbx) return;

            scrollDefaultAndShuffleSongsLbx = false;

            ScrollListboxToCurrentSong(defaultSongsLbx);
            ScrollListboxToCurrentSong(shuffleSongsLbx);
        }

        private void ScrollListboxToCurrentSong(ListBox lbx)
        {
            if (lbx == null || !lbx.Items.Contains(CurrentSong)) return;

            lbx.ScrollIntoView(CurrentSong);
        }

        private void SetScrollDefaultAndShuffleSongsLbx()
        {
            scrollDefaultAndShuffleSongsLbx = true;
        }

        public static string GetRelativePath(string absolutePath)
        {
            if (absolutePath == "") return "\\Music";
            int index = absolutePath.IndexOf("\\Music");

            return absolutePath.Remove(0, index);
        }

        private int GetPossibleSongsIndex(int inIndex)
        {
            if (inIndex >= 0 && inIndex < Songs.Count && Songs.Count > 0) return inIndex;

            return inIndex < 0 ? 0 : Songs.Count - 1;
        }

        private bool IsShuffleListIndex(int index)
        {
            return ShuffleList.Count > index && index >= 0;
        }

        private void ChangeShuffleListBecauseOfChangedSongIndex(int index)
        {
            int offset, offsetAbs;
            List<int> updatedShuffleList;

            if (index == SongsIndex) return;

            if (Shuffle == ShuffleKind.Complete)
            {
                updatedShuffleList = new List<int>(ShuffleList);
                AddSongsIndexToShuffleListAndRemoveFirst(index, ref updatedShuffleList);

                offset = updatedShuffleList.IndexOf(index) - ShuffleListIndex;
                offsetAbs = Math.Abs(offset);

                if (offset < 0) updatedShuffleList.Reverse();
                AddRandomIndexToUpdatedShuffleListAndRemoveFirstOnes(offsetAbs, ref updatedShuffleList);
                if (offset < 0) updatedShuffleList.Reverse();

                shuffleList = updatedShuffleList;
            }
        }

        private void AddRandomIndexToUpdatedShuffleListAndRemoveFirstOnes(int count, ref List<int> updatedShuffleList)
        {
            for (int i = 0; i < count; i++)
            {
                updatedShuffleList.RemoveAt(0);
                AddRandomIndexWhichIsNotInShuffleList(ref updatedShuffleList, Songs);
            }
        }

        private void AddSongsIndexToShuffleListAndRemoveFirst(int songsIndex, ref List<int> updatedShuffleList)
        {
            if (!updatedShuffleList.Contains(songsIndex))
            {
                updatedShuffleList.RemoveAt(0);
                updatedShuffleList.Add(songsIndex);
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

        public bool ChangeCurrentSong(int offset)
        {
            int offsetAbs = Math.Abs(offset);
            ShuffleListIndex = GetIndexInRange(ShuffleListIndex + offset, ShuffleList.Count);

            if (CurrentSong.Failed)
            {
                if (Songs.Where(x => !x.Failed).ToArray().Length == 0) return true;

                return ChangeCurrentSong(offset);
            }

            return Loop != LoopKind.On && !IsShuffleListIndex(ShuffleListIndex - offset);
        }

        private int GetIndexInRange(int index, int length)
        {
            return ((index) % length + length) % length;
        }

        private void AddRandomIndexWhichIsNotInShuffleList(ref List<int> shuffleList, List<Song> songs)
        {
            int index;

            do
            {
                index = ran.Next(songs.Count);

            } while (shuffleList.Contains(index));

            shuffleList.Add(index);
        }

        public void SetNextShuffle()
        {
            switch (Shuffle)
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

            GenerateShuffleList();
        }

        public void SetNextLoop()
        {
            switch (Loop)
            {
                case LoopKind.Off:
                    Loop = LoopKind.On;
                    break;

                case LoopKind.On:
                    Loop = LoopKind.Current;
                    break;

                case LoopKind.Current:
                    Loop = LoopKind.Off;
                    break;
            }

            if (Library.Current.IsForeground) BackgroundCommunicator.SendLoop(this);
        }

        public void GenerateShuffleList()
        {
            switch (Shuffle)
            {
                case ShuffleKind.Off:
                    shuffleList = GetShuffleListOff(Songs.Count);
                    break;

                case ShuffleKind.OneTime:
                    shuffleList = GetShuffleListOneTime(new List<int>() { SongsIndex }, Songs);
                    break;

                case ShuffleKind.Complete:
                    shuffleList = GetShuffleListComplete();
                    break;
            }

            if (Library.Current.IsForeground)
            {
                BackgroundCommunicator.SendShuffle(this);

                ViewModel.Current.SetScrollLbxCurrentPlaylist();
                SetScrollDefaultAndShuffleSongsLbx();
                UpdateCurrentSongIndex();
            }
        }

        private List<int> GetShuffleListOff(int count)
        {
            List<int> list = new List<int>();

            for (int i = 0; i < count; i++)
            {
                list.Add(i);
            }

            return list;
        }

        private List<int> GetShuffleListOneTime(List<int> currentShuffleList, List<Song> updatedSongs)
        {
            List<int> list = GetUpdatedShuffleListForUpdatedSongs(currentShuffleList, updatedSongs);
            GetUpdatedShuffleListWithAddedSongs(currentShuffleList, ref list, updatedSongs);

            return list;
        }

        private List<int> GetShuffleListComplete()
        {
            int shuffleCompleteCurrentIndex = GetShuffleCompleteCurrentIndex(Songs.Count);
            int shuffleCompleteCount = GetShuffleCompleteCount(Songs.Count);
            List<int> updatedShuffleList = new List<int>();

            for (int i = 0; i < shuffleCompleteCount; i++)
            {
                if (i != shuffleCompleteCurrentIndex) AddRandomIndexWhichIsNotInShuffleList(ref updatedShuffleList, Songs);
                else updatedShuffleList.Add(SongsIndex);
            }

            return updatedShuffleList;
        }

        private List<int> GetShuffleListComplete(List<Song> updatedSongs)
        {
            int isShuffleListIndex, willBeShuffleListIndex, willBeShuffleListCount;
            List<int> updatedShuffleList = GetUpdatedShuffleListForUpdatedSongs(ShuffleList, updatedSongs);

            if (updatedShuffleList.Count == GetShuffleCompleteCount(updatedSongs.Count)) return updatedShuffleList;

            isShuffleListIndex = GetShuffleCompleteCurrentIndex(Songs.Count);
            willBeShuffleListIndex = GetShuffleCompleteCurrentIndex(updatedSongs.Count);
            willBeShuffleListCount = GetShuffleCompleteCount(Songs.Count);

            AddRandomIndexesInFrontOfUpdatedShuffleList(willBeShuffleListIndex - isShuffleListIndex, ref updatedShuffleList);
            AddRandomIndexesToEndOfUpdatedShuffleList(willBeShuffleListCount - updatedShuffleList.Count, ref updatedShuffleList);

            return updatedShuffleList;
        }

        private void AddRandomIndexesInFrontOfUpdatedShuffleList(int count, ref List<int> updatedShuffleList)
        {
            if (count == 0) return;

            updatedShuffleList.Reverse();
            AddRandomIndexesToEndOfUpdatedShuffleList(count, ref updatedShuffleList);
            updatedShuffleList.Reverse();
        }

        private void AddRandomIndexesToEndOfUpdatedShuffleList(int count, ref List<int> updatedShuffleList)
        {
            for (int i = 0; i < count; i++)
            {
                AddRandomIndexWhichIsNotInShuffleList(ref updatedShuffleList, Songs);
            }
        }

        private List<Song> SetCurrentSongsOrderedWithAddedSongs(List<Song> currentSongs, List<Song> addSongs)
        {
            if (addSongs.Count == 0) return currentSongs;

            List<Song> updatedSongs = new List<Song>(currentSongs);
            updatedSongs.AddRange(addSongs);

            return updatedSongs.OrderBy(x => x.Title).ThenBy(x => x.Artist).ToList();
        }

        private List<int> GetUpdatedShuffleListForUpdatedSongs(List<int> currentShuffleList, List<Song> updatedSongs)
        {
            List<int> updatedShuffleList = new List<int>();

            foreach (int songsIndex in currentShuffleList)
            {
                updatedShuffleList.Add(updatedSongs.IndexOf(Songs[songsIndex]));
            }

            return updatedShuffleList;
        }

        private void GetUpdatedShuffleListWithAddedSongs(List<int> currentShuffleList,
            ref List<int> updatedShuffleList, List<Song> updatedSongs)
        {
            for (int i = currentShuffleList.Count; i < updatedSongs.Count; i++)
            {
                AddRandomIndexWhichIsNotInShuffleList(ref updatedShuffleList, updatedSongs);
            }
        }

        private int GetShuffleCompleteCount(int songsCount)
        {
            int count = shuffleCompleteListNextCount + shuffleCompleteListPreviousCount + 1;
            return songsCount > count ? count : songsCount;
        }

        private int GetShuffleCompleteCurrentIndex(int songsCount)
        {
            double indexDouble = (GetShuffleCompleteCount(songsCount) - 1) /
                Convert.ToDouble(shuffleCompleteListNextCount + shuffleCompleteListPreviousCount) *
                shuffleCompleteListPreviousCount;

            return Convert.ToInt32(indexDouble);
        }

        public void RemoveSong(int songsIndex)
        {
            if (IsEmptyOrLoading) return;

            int shuffleListIndex = ShuffleList.IndexOf(songsIndex);
            List<int> updatedShuffleList = new List<int>(ShuffleList);
            List<Song> updatedSongs = new List<Song>(Songs);

            updatedSongs.RemoveAt(songsIndex);
            songs = updatedSongs;

            if (shuffleListIndex != -1)
            {
                if (SongsIndex > songsIndex) this.songsIndex = SongsIndex - 1;

                if (Shuffle == ShuffleKind.Off)
                {
                    shuffleList.RemoveAt(shuffleList.Count - 1);
                }
                else
                {
                    updatedShuffleList.RemoveAt(shuffleListIndex);

                    if (Shuffle == ShuffleKind.OneTime)
                    {
                        IncreaseEveryIndexOfShuffleListOverSameIndexAndSet(updatedShuffleList, shuffleListIndex);
                    }
                    else AddRandomIndexToUpadetedShuffleListAndSet(updatedShuffleList, shuffleListIndex);
                }
            }

            if (Library.Current.IsForeground)
            {
                BackgroundCommunicator.SendRemoveSong(PlaylistIndex, songsIndex);

                UpdateSongsAndShuffleListSongs();
                UpdateCurrentSong();
            }
        }

        private void IncreaseEveryIndexOfShuffleListOverSameIndexAndSet(List<int> updatedShuffleList, int index)
        {
            for (int i = 0; i < updatedShuffleList.Count; i++)
            {
                if (updatedShuffleList[i] >= index)
                {
                    updatedShuffleList[i]--;
                }
            }

            shuffleList = updatedShuffleList;
        }

        private void AddRandomIndexToUpadetedShuffleListAndSet(List<int> updatedShuffleList, int index)
        {
            if (updatedShuffleList.Count != songs.Count)
            {
                bool first = ShuffleListIndex > index;

                if (first) updatedShuffleList.Reverse();
                AddRandomIndexWhichIsNotInShuffleList(ref updatedShuffleList, Songs);
                if (first) updatedShuffleList.Reverse();
            }

            IncreaseEveryIndexOfShuffleListOverSameIndexAndSet(updatedShuffleList, index);
        }

        public async Task<StorageFolder> GetStorageFolder()
        {
            if (absolutePath == "") return KnownFolders.MusicLibrary;

            return await StorageFolder.GetFolderFromPathAsync(absolutePath);
        }

        private async Task<IReadOnlyList<StorageFile>> GetStorageFolderFiles()
        {
            var folder = await GetStorageFolder();

            return await folder.GetFilesAsync();
        }

        public async Task LoadSongsFromStorage()
        {
            var files = await GetStorageFolderFiles();
            List<Song> updatedSongs = SetCurrentSongsOrderedWithAddedSongs(new List<Song>(), await GetSongsFromStorageFiles(files));

            if (Library.Current.CanceledLoading) return;

            songs = updatedSongs;
            Shuffle = ShuffleKind.Off;
            GenerateShuffleList();

            SongsIndex = 0;

            if (IsEmptyOrLoading)
            {
                Library.Current.Delete(this);
                return;
            }
        }

        private async Task<List<Song>> GetSongsFromStorageFiles(IReadOnlyList<StorageFile> files)
        {
            Song addSong;
            List<Song> list = new List<Song>();

            foreach (StorageFile file in files)
            {
                if (Library.Current.CanceledLoading) return list;

                addSong = new Song(file.Path);
                await addSong.Refresh();

                list.Add(addSong);
            }

            return list;
        }

        public async Task SearchForNewSongs()
        {
            var files = await GetStorageFolderFiles();
            List<StorageFile> addFiles = GetStorageFilesWhichAreNotInSongs(files);

            if (addFiles.Count == 0) return;

            SetSongsAndShuffleListWithAddSongs(await GetSongsFromStorageFiles(addFiles));

            UpdateAndSendToBackground();
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
            List<int> updatedShuffleList;
            List<Song> updatedSongs = SetCurrentSongsOrderedWithAddedSongs(songs, addSongs);

            switch (Shuffle)
            {
                case ShuffleKind.Off:
                    updatedShuffleList = GetShuffleListOff(updatedSongs.Count);
                    break;

                case ShuffleKind.OneTime:
                    updatedShuffleList = GetShuffleListOneTime(shuffleList, updatedSongs);
                    break;

                case ShuffleKind.Complete:
                    updatedShuffleList = GetShuffleListComplete(updatedSongs);
                    break;

                default:
                    return;
            }

            songs = updatedSongs;
            shuffleList = updatedShuffleList;
        }

        private void UpdateAndSendToBackground()
        {
            if (Library.Current.IsForeground) BackgroundCommunicator.SendPlaylistXML(this);

            UpdateSongsAndShuffleListSongs();
            UpdateCurrentSongIndex();
            UpdateSongCount();
            UpdateCurrentSong();
            ViewModel.Current.UpdateSliderMaximumAndSliderValue();
        }

        public void UpdateName()
        {
            NotifyPropertyChanged("Name");
        }

        public void UpdateLoopIcon()
        {
            NotifyPropertyChanged("LoopIcon");
        }

        public void UpdateShuffleIcon()
        {
            NotifyPropertyChanged("ShuffleIcon");
        }

        private void UpdateSongCount()
        {
            NotifyPropertyChanged("SongCount");
        }

        public void UpdateCurrentSong()
        {
            UpdateCurrentSongIndex();

            NotifyPropertyChanged("CurrentSong");
            CurrentSong.UpdateTitleAndArtist();

            if (Library.Current.IsForeground) ViewModel.Current.UpdateSliderMaximumAndSliderValue();
            else { }
        }

        private void UpdateCurrentSongIndex()
        {
            NotifyPropertyChanged("SongsIndex");
            NotifyPropertyChanged("ShuffleListIndex");
        }

        public void UpdateSongsAndShuffleListSongs()
        {
            NotifyPropertyChanged("Songs");
            NotifyPropertyChanged("ShuffleSongs");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void NotifyPropertyChanged(string propertyName)
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
            return Name;
        }
    }
}
