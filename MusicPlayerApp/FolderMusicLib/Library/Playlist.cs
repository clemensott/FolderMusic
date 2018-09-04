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
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace LibraryLib
{
    public enum LoopKind { Off, All, Current };

    public enum ShuffleKind { Off, OneTime, Complete };

    public class Playlist : INotifyPropertyChanged
    {
        private static Brush textFirstBrush;
        private static Brush textSecondBrush;

        private const string emptyOrLoadingPath = "None";

        private int songsIndex = 0;
        private double songPostionMilliseconds;
        private string name, absolutePath;
        private ILoop iLoop = new LoopOff();
        private IShuffle iShuffle = new ShuffleOff();
        private List<int> shuffleList;
        private List<Song> songs;

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

        [XmlIgnore]
        public int PlaylistIndex { get { return Library.Current.GetPlaylistIndex(this); } }

        public int SongsIndex
        {
            get { return GetPossibleSongsIndex(songsIndex); }
            set
            {
                if (SongsIndex == value || value == -1) return;

                if (!IsEmptyOrLoading)
                {
                    shuffleList = iShuffle.GetChangedShuffleListBecauseOfAnotherSongsIndex(value, shuffleList, Songs.Count);
                    songPostionMilliseconds = 0;
                }

                songsIndex = value;

                if (Library.Current.IsForeground && Library.IsLoaded && !IsEmptyOrLoading)
                {
                    BackgroundCommunicator.SendPlaylistsAndSongsIndexAndShuffleIfComplete(this);
                    Library.Current.CurrentPlaylistIndex = PlaylistIndex;

                    UpdateCurrentSong();
                }
            }
        }

        [XmlIgnore]
        public int ShuffleListIndex
        {
            get { return iShuffle.GetShuffleListIndex(SongsIndex, ShuffleList, Songs.Count); }
            set
            {
                if (value == -1)
                {
                    UpdateCurrentSongIndex();
                    return;
                }

                if (!IsShuffleListIndex(value)) return;

                SongsIndex = ShuffleList[value];
            }
        }

        [XmlIgnore]
        public string SongCount { get { return songs.Count != 1 ? songs.Count.ToString() + " Songs" : "1 Song"; } }

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
        public static Brush TextFirstBrush
        {
            get
            {
                if (textFirstBrush == null)
                {
                    textFirstBrush = Icons.Theme == ElementTheme.Light ? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) :
                      new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                }

                return textFirstBrush;
            }
        }

        [XmlIgnore]
        public static Brush TextSecondBrush
        {
            get
            {
                if (textSecondBrush == null)
                {
                    textSecondBrush = Icons.Theme == ElementTheme.Light ? new SolidColorBrush(Color.FromArgb(255, 127, 127, 127)) :
                      new SolidColorBrush(Color.FromArgb(255, 192, 192, 192));
                }

                return textSecondBrush;
            }
        }

        [XmlIgnore]
        public BitmapImage LoopIcon { get { return iLoop.GetIcon(); } }

        [XmlIgnore]
        public BitmapImage ShuffleIcon { get { return iShuffle.GetIcon(); } }

        [XmlIgnore]
        public BitmapImage PlayIcon { get { return Library.Current.IsEmpty ? new BitmapImage() : Icons.PlayImage; } }

        [XmlIgnore]
        public Song CurrentSong { get { return !IsEmptyOrLoading ? Songs[SongsIndex] : new Song(); } }

        public List<Song> Songs { get { return songs; } }

        [XmlIgnore]
        public List<Song> ShuffleSongs
        {
            get
            {
                if (IsEmptyOrLoading) return new List<Song>() { new Song() };

                List<Song> list = new List<Song>();

                foreach (int i in ShuffleList) list.Add(Songs[i]);

                return list;
            }
        }

        public LoopKind Loop
        {
            get { return iLoop.GetKind(); }
            set
            {
                if (Loop == value) return;

                switch (value)
                {
                    case LoopKind.All:
                        iLoop = new LoopAll();
                        break;

                    case LoopKind.Current:
                        iLoop = new LoopCurrent();
                        break;

                    default:
                        iLoop = new LoopOff();
                        break;
                }

                UpdateLoopIcon();
            }
        }

        public ShuffleKind Shuffle
        {
            get { return iShuffle.GetKind(); }
            set
            {
                if (Shuffle == value) return;

                switch (value)
                {
                    case ShuffleKind.Complete:
                        iShuffle = new ShuffleComplete();
                        break;

                    case ShuffleKind.OneTime:
                        iShuffle = new ShuffleOneTime();
                        break;

                    default:
                        iShuffle = new ShuffleOff();
                        break;
                }

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

        public Playlist(CurrentSong currentSong)
        {
            Name = Library.IsLoaded ? "None" : "Loading";
            absolutePath = emptyOrLoadingPath;

            Loop = LoopKind.Off;
            Shuffle = ShuffleKind.Off;

            songs = new List<Song>() { currentSong.Song };
            shuffleList = new List<int>() { 0 };

            songsIndex = 0;
            songPostionMilliseconds = currentSong.PositionMilliseconds;
        }

        public static string GetRelativePath(string absolutePath)
        {
            if (absolutePath == "") return "\\Music";
            int index = absolutePath.IndexOf("\\Music");

            return absolutePath.Remove(0, index);
        }

        public void UpdateSongsObject()
        {
            songs = new List<Song>(songs);
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
            if (ShuffleList.Count == 0) return true;
            ShuffleListIndex = GetIndexInRange(ShuffleListIndex + offset, ShuffleList.Count);

            if (CurrentSong.Failed)
            {
                if (Songs.Where(x => !x.Failed).ToArray().Length == 0) return true;

                return ChangeCurrentSong(offset);
            }

            return Loop != LoopKind.All && !IsShuffleListIndex(ShuffleListIndex - offset);
        }

        private int GetIndexInRange(int index, int length)
        {
            return ((index) % length + length) % length;
        }

        public void SetNextShuffle()
        {
            iShuffle = iShuffle.GetNext();

            GenerateShuffleList();
            UpdateShuffleIcon();
        }

        public void SetNextLoop()
        {
            iLoop = iLoop.GetNext();

            if (Library.Current.IsForeground)
            {
                BackgroundCommunicator.SendLoop(this);
                UpdateLoopIcon();
            }
        }

        public void GenerateShuffleList()
        {
            shuffleList = iShuffle.GenerateShuffleList(SongsIndex, Songs.Count);

            if (Library.Current.IsForeground)
            {
                BackgroundCommunicator.SendShuffle(this);

                UpdateSongsAndShuffleListSongs();
                UpdateCurrentSongIndex();
            }

            Library.Current.FireScrollEvent(this);
        }

        private List<Song> SetCurrentSongsOrderedWithAddedSongs(List<Song> currentSongs, List<Song> addSongs)
        {
            if (addSongs.Count == 0) return currentSongs;

            List<Song> updatedSongs = new List<Song>(currentSongs);
            updatedSongs.AddRange(addSongs);

            return GetOrderedSongs(updatedSongs);
        }

        private List<Song> GetOrderedSongs(List<Song> songs)
        {
            return songs.OrderBy(x => x.Title).ThenBy(x => x.Artist).ToList();
        }

        public void RemoveSong(int songsIndex)
        {
            if (IsEmptyOrLoading) return;

            List<Song> updatedSongs = new List<Song>(Songs);
            updatedSongs.RemoveAt(songsIndex);

            shuffleList = iShuffle.RemoveSongsIndex(songsIndex, shuffleList, updatedSongs.Count);
            songs = updatedSongs;

            if (SongsIndex > songsIndex) this.songsIndex--;

            if (Library.Current.IsForeground)
            {
                BackgroundCommunicator.SendRemoveSong(PlaylistIndex, songsIndex);

                UpdateSongsAndShuffleListSongs();
                UpdateCurrentSong();
            }

            Library.Current.DeleteEmptyPlaylists();
        }

        private async Task<StorageFolder> GetStorageFolder()
        {
            if (absolutePath == "") return KnownFolders.MusicLibrary;

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

        public async Task LoadSongsFromStorage()
        {
            List<StorageFile> files = await GetStorageFolderFiles();
            List<Song> foundSongs = await GetSongsFromStorageFiles(files);
            List<Song> updatedSongs = SetCurrentSongsOrderedWithAddedSongs(new List<Song>(), foundSongs);

            if (Library.Current.CanceledLoading) return;

            songs = updatedSongs;
            Shuffle = ShuffleKind.Off;
            GenerateShuffleList();

            songsIndex = 0;

            if (IsEmptyOrLoading) Library.Current.Delete(this);
            else UpdateAndSendToBackground();
        }

        private async Task<List<Song>> GetSongsFromStorageFiles(List<StorageFile> files)
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

        public async Task UpdateSongsFromStorage()
        {
            Song addSong;
            List<string> songsPath = Songs.Select(x => x.Path).ToList();
            List<StorageFile> files = await GetStorageFolderFiles();
            List<Song> updatedSongs = new List<Song>();

            foreach (StorageFile file in files)
            {
                if (Library.Current.CanceledLoading) return;

                if (songsPath.Contains(file.Path)) updatedSongs.Add(Songs[songsPath.IndexOf(file.Path)]);
                else
                {
                    addSong = new Song(file.Path);
                    await addSong.Refresh();

                    updatedSongs.Add(addSong);
                }
            }

            addSong = CurrentSong;
            songs = GetOrderedSongs(updatedSongs);
            shuffleList = iShuffle.AddSongsToShuffleList(ShuffleList, Songs, updatedSongs);
            songsIndex = Songs.IndexOf(addSong);

            Library.Current.DeleteEmptyPlaylists();
            UpdateAndSendToBackground();
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
            List<Song> updatedSongs = SetCurrentSongsOrderedWithAddedSongs(songs, addSongs);
            List<int>  updatedShuffleList = iShuffle.AddSongsToShuffleList(shuffleList, Songs, updatedSongs);

            songs = updatedSongs;
            shuffleList = updatedShuffleList;
        }

        private void UpdateAndSendToBackground()
        {
            if (!Library.Current.IsForeground) return;

            BackgroundCommunicator.SendPlaylistXML(this);
            UpdateSongsAndShuffleListSongs();
            UpdateCurrentSongIndex();
            UpdateSongCount();
            UpdateCurrentSong();
            ViewModel.Current.ChangeSliderMaximumAndSliderValue();
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

            ViewModel.Current.ChangeSliderMaximumAndSliderValue();
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
            if (null == PropertyChanged) return;

            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.
                    RunAsync(CoreDispatcherPriority.Normal, () => 
                    {
                        if (null == PropertyChanged) return;

                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    });
            }
            catch { }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
