using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;

namespace LibraryLib
{
    public class Library
    {
        private static Library instance;

        private volatile bool isSaveing = false, saveAgain = false, cancelLoading = false;
        private static bool loaded;
        private static string currentSongMillisecondsFileName = "CurrentSongMilliseconds.txt",
          currentSongFileName = "currentSong.xml", skipSongsFileName = "SkipSongs.xml", playCommandFileName = "PlayCommand.txt";

        private int currentPlaylistIndex = 0;
        private double currentSongPositionMilliseconds;
        private Song currentSong = new Song();
        private List<Playlist> _playlists;
        private readonly List<Playlist> noPlaylists = new List<Playlist>() { new Playlist() };

        public bool CanceledLoading { get { return cancelLoading; } }

        public bool IsEmpty { get { return _playlists.Count == 0; ; } }

        public static bool IsLoaded { get { return loaded; } }

        public static Library Current
        {
            get
            {
                if (instance == null) instance = new Library();

                return instance;
            }
        }

        private List<Playlist> playlists
        {
            get { return IsLoaded && !IsEmpty ? _playlists : noPlaylists; }
        }

        public Playlist this[int index]
        {
            get { return playlists[index]; }
            set { playlists[index] = value; }
        }

        public int Length { get { return playlists.Count; } }

        public int CurrentPlaylistIndex
        {
            get { return GetPossibleCurrentPlaylistIndex(currentPlaylistIndex); }
            set
            {
                if (CurrentPlaylistIndex == value) return;

                CurrentPlaylist.SongPositionMilliseconds = BackgroundMediaPlayer.Current.Position.TotalMilliseconds;
                currentPlaylistIndex = value;
            }
        }

        public double CurrentSongPositionMilliseconds
        {
            get { return IsLoaded ? CurrentPlaylist.SongPositionMilliseconds : currentSongPositionMilliseconds; }
        }

        public Song CurrentSong { get { return GetCurrentSong(); } }

        public Playlist CurrentPlaylist { get { return this[CurrentPlaylistIndex]; } }

        private Library()
        {
            _playlists = new List<Playlist>();
        }

        private int GetPossibleCurrentPlaylistIndex(int inIndex)
        {
            if (playlists.Count == 0) return -1;

            if (inIndex >= 0 && inIndex < playlists.Count && playlists.Count > 0) return inIndex;

            return inIndex < 0 ? 0 : playlists.Count - 1;
        }

        public void Load(string xmlText)
        {
            SaveClass sc = XmlConverter.Deserialize<SaveClass>(xmlText);

            _playlists = sc.Playlists;
            currentPlaylistIndex = sc.CurrentPlaylistIndex;

            loaded = true;
        }

        public async Task LoadAsync()
        {
            await LoadCurrentSong();
            await LoadNonStatic();

            SetCurrentSong();
            CurrentPlaylist.SongPositionMilliseconds = currentSongPositionMilliseconds;
        }

        private void SetCurrentSong()
        {
            Song[] songs = CurrentPlaylist.Songs.Where(x => x.Path == currentSong.Path).ToArray();

            if (songs.Length != 1) return;

            CurrentPlaylist.SongsIndex = CurrentPlaylist.Songs.IndexOf(songs[0]);
        }

        private async Task LoadNonStatic()
        {
            SaveClass sc = await SaveClass.Load();
            _playlists = sc.Playlists;

            loaded = true;
            currentPlaylistIndex = sc.CurrentPlaylistIndex == -2 ? 0 : sc.CurrentPlaylistIndex;
        }

        private async Task LoadCurrentSongMilliseconds()
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(currentSongMillisecondsFileName);
                string text = await PathIO.ReadTextAsync(file.Path);

                if (double.TryParse(text, out currentSongPositionMilliseconds)) { }
            }
            catch { }
        }

        public async Task LoadPlaylistsFromStorage()
        {
            await DeleteCurrentSongMillisecondsFile();
            await DeleteCurrentSongFile();
            List<Playlist> list = new List<Playlist>(await LoadPlaylistsFromStorage(KnownFolders.MusicLibrary));

            foreach (Playlist playlist in list)
            {
                await playlist.LoadSongsFromStorage();

                if (CanceledLoading)
                {
                    cancelLoading = false;
                    return;
                }
            }

            _playlists = new List<Playlist>(list);
            DeleteEmptyPlaylists();

            currentPlaylistIndex = 0;
        }

        private async Task<List<Playlist>> LoadPlaylistsFromStorage(StorageFolder folder)
        {
            List<Playlist> list = new List<Playlist>();

            try
            {
                list.Add(new Playlist(folder.Path));

                var folders = await folder.GetFoldersAsync();

                foreach (StorageFolder listFolder in folders)
                {
                    list.AddRange(await LoadPlaylistsFromStorage(listFolder));
                }
            }
            catch { }

            return list;
        }

        public string GetXmlText()
        {
            SaveClass sc = new SaveClass(CurrentPlaylistIndex, playlists);

            return XmlConverter.Serialize(sc);
        }

        public async Task SaveAsync()
        {
            if (IsEmpty) return;
            saveAgain = true;

            if (isSaveing) return;
            isSaveing = true;

            while (saveAgain)
            {
                saveAgain = false;
                SaveClass sc = new SaveClass(CurrentPlaylistIndex, playlists);

                await sc.Save();
            }

            isSaveing = false;
        }

        public static async void SavePlayCommand(bool command)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + playCommandFileName;

            try
            {
                await PathIO.WriteTextAsync(path, command.ToString());
            }
            catch (FileNotFoundException)
            {
                try
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(playCommandFileName);
                    await PathIO.WriteTextAsync(path, command.ToString());
                }
                catch { }
            }
            catch { }
        }

        public static async Task<bool> LoadPlayCommand()
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + playCommandFileName;
            string text = true.ToString();

            try
            {
                text = await PathIO.ReadTextAsync(path);
            }
            catch { }

            return bool.Parse(text);
        }

        public List<Playlist> GetPlaylists()
        {
            return playlists;
        }

        public int GetPlaylistIndex(Playlist playlist)
        {
            return playlists.IndexOf(playlist);
        }

        public async Task SaveCurrentSongMilliseconds()
        {
            if (!IsLoaded) return;

            double milliseconds = BackgroundMediaPlayer.Current.Position.TotalMilliseconds;
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + currentSongMillisecondsFileName;

            try
            {
                await PathIO.WriteTextAsync(path, milliseconds.ToString());
            }
            catch (FileNotFoundException)
            {
                try
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(currentSongMillisecondsFileName);
                    await PathIO.WriteTextAsync(path, milliseconds.ToString());
                }
                catch { }
            }
            catch { }

            await SaveCurrentSong();
        }

        private static async Task DeleteCurrentSongMillisecondsFile()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(currentSongMillisecondsFileName);

                await file.DeleteAsync();
            }
            catch { }
        }

        private Song GetCurrentSong()
        {
            return IsLoaded ? CurrentPlaylist.CurrentSong : currentSong;
        }

        private async Task SaveCurrentSong()
        {
            string xmlFileText = XmlConverter.Serialize(CurrentPlaylist.CurrentSong);
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + currentSongFileName;

            if (!IsLoaded) return;

            try
            {
                await PathIO.WriteTextAsync(path, xmlFileText);
            }
            catch (FileNotFoundException)
            {
                try
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(currentSongFileName);
                    await PathIO.WriteTextAsync(path, xmlFileText);
                }
                catch { }
            }
            catch { }
        }

        public async Task LoadCurrentSong()
        {
            string path, xmlFileText;

            try
            {
                path = ApplicationData.Current.LocalFolder.Path + "\\" + currentSongFileName;

                xmlFileText = await PathIO.ReadTextAsync(path);
                currentSong = XmlConverter.Deserialize<Song>(xmlFileText);
            }
            catch
            {
                currentSong = new Song();
            }

            await LoadCurrentSongMilliseconds();
        }

        private static async Task DeleteCurrentSongFile()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(currentSongFileName);

                await file.DeleteAsync();
            }
            catch { }
        }

        public async static Task AddSkipSongAndSave(Song song)
        {
            List<Song> list = await LoadSkipSongs();

            foreach (Song saveSong in list)
            {
                if (saveSong.Path == song.Path) return;
            }

            list.Add(song);

            await SaveSkipSongs(list);
        }

        public async static Task RemoveSkipSongAndSave(List<Song> list, Song saveSong)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Path == saveSong.Path) list.RemoveAt(i);
            }

            await SaveSkipSongs(list);
        }

        private async static Task SaveSkipSongs(List<Song> list)
        {
            string xmlFileText = XmlConverter.Serialize(list);
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + skipSongsFileName;

            try
            {
                await PathIO.WriteTextAsync(path, xmlFileText);
            }
            catch (FileNotFoundException)
            {
                try
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(skipSongsFileName);
                    await PathIO.WriteTextAsync(path, xmlFileText);
                }
                catch { }
            }
            catch { }
        }

        public async static Task<List<Song>> LoadSkipSongs()
        {
            string path, xmlFileText;
            List<Song> saveSongsList = new List<Song>();

            try
            {
                path = ApplicationData.Current.LocalFolder.Path + "\\" + skipSongsFileName;

                xmlFileText = await PathIO.ReadTextAsync(path);
                saveSongsList = XmlConverter.Deserialize<List<Song>>(xmlFileText);
            }
            catch { }

            return saveSongsList;
        }

        public void RemoveSongFromPlaylist(Playlist playlist, int songsIndex)
        {
            playlist.RemoveSong(songsIndex);
            DeleteEmptyPlaylists();
        }

        private void DeleteEmptyPlaylists()
        {
            for (int i = playlists.Count - 1; i >= 0; i--)
            {
                if (playlists[i].IsEmptyOrLoading) DeleteAt(i);
            }
        }

        public void Delete(Playlist playlist)
        {
            if (IsEmpty) return;

            Playlist oldCurrentPlaylist = CurrentPlaylist;
            _playlists.Remove(playlist);
            _playlists = new List<Playlist>(_playlists);

            SetPlaylistToCurrentPlaylist(oldCurrentPlaylist);
        }

        public void DeleteAt(int index)
        {
            if (IsEmpty) return;

            Playlist oldCurrentPlaylist = CurrentPlaylist;
            _playlists.RemoveAt(index);
            _playlists = new List<Playlist>(_playlists);

            SetPlaylistToCurrentPlaylist(oldCurrentPlaylist);
        }

        public int GetPlaylistIndexWhichContainsSong(Song song)
        {
            var playlistsWithSong = _playlists.Where(x => x.Songs.Contains(song)).ToList();

            if (playlistsWithSong.Count != 1) return -1;
            return _playlists.IndexOf(playlistsWithSong[0]);
        }

        private void SetPlaylistToCurrentPlaylist(Playlist playlist)
        {
            int newCurrentPlaylistIndex = _playlists.IndexOf(playlist);

            if (newCurrentPlaylistIndex != -1 && CurrentPlaylistIndex != newCurrentPlaylistIndex)
            {
                CurrentPlaylistIndex = newCurrentPlaylistIndex;
            }
        }

        public async Task SearchForNewPlaylists()
        {
            bool existsAllready;
            int newCurrentPlaylistIndexCounter, newCurrentPlaylistIndex;
            string currentPlaylistAbsolutePath = CurrentPlaylist.AbsolutePath;

            newCurrentPlaylistIndex = newCurrentPlaylistIndexCounter = CurrentPlaylistIndex;

            List<Playlist> possiblePlaylists = new List<Playlist>(await LoadPlaylistsFromStorage(KnownFolders.MusicLibrary));
            List<Playlist> exceptPlaylist = new List<Playlist>();

            for (int i = 0; i < possiblePlaylists.Count; i++)
            {
                existsAllready = false;

                foreach (Playlist playlist in playlists)
                {
                    if (CanceledLoading)
                    {
                        cancelLoading = false;
                        return;
                    }

                    if (possiblePlaylists[i].AbsolutePath == playlist.AbsolutePath)
                    {
                        exceptPlaylist.Add(playlist);
                        existsAllready = true;

                        if (playlist.AbsolutePath == currentPlaylistAbsolutePath) newCurrentPlaylistIndex = newCurrentPlaylistIndexCounter;
                        break;
                    }
                }

                if (!existsAllready)
                {
                    await possiblePlaylists[i].LoadSongsFromStorage();

                    if (CanceledLoading)
                    {
                        cancelLoading = false;
                        return;
                    }

                    if (!possiblePlaylists[i].IsEmptyOrLoading)
                    {
                        newCurrentPlaylistIndexCounter++;
                        exceptPlaylist.Add(possiblePlaylists[i]);
                    }
                }
            }

            _playlists = exceptPlaylist;
            CurrentPlaylistIndex = newCurrentPlaylistIndex;
        }

        public void CancelLoading()
        {
            cancelLoading = true;
        }
    }
}
