using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;

namespace PlaylistSong
{
    public delegate void LoadedEventHandler();

    public class Library
    {
        private static Library instance;

        private volatile static bool isSaveing = false, saveAgain = false;
        private static string currentSongIndexFileName = "CurrentSongIndex.txt", playCommandFileName = "PlayCommand.txt";

        private int currentPlaylistIndex;
        private List<Playlist> _playlists;

        public static Library Current
        {
            get
            {
                if (instance == null)
                {
                    instance = new Library();
                }

                return instance;
            }
        }

        private List<Playlist> playlists
        {
            get { return _playlists.Count > 0 ? _playlists :
                    new List<Playlist>() { new Playlist() }; }

            set { _playlists = value; }
        }

        public Playlist this[int index] { get { return playlists[index]; } }

        public int Lenght { get { return playlists.Count; } }

        public bool HaveToLoad { get; set; }

        public int CurrentPlaylistIndex
        {
            get { return currentPlaylistIndex; }
            set
            {
                value = GetPossibleCurrentPlaylistIndex(value);

                if (currentPlaylistIndex != value)
                {
                    CurrentPlaylist.SongPositionMilliseconds = BackgroundMediaPlayer.Current.Position.TotalMilliseconds;
                    currentPlaylistIndex = value;
                }
            }
        }

        public Playlist CurrentPlaylist
        {
            get { return HaveCurrentPlaylist() ? this[currentPlaylistIndex] : new Playlist(); }
        }

        private Library()
        {
            playlists = new List<Playlist>();
            LoadNonStatic();
        }

        private int GetPossibleCurrentPlaylistIndex(int inIndex)
        {
            if (playlists.Count == 0) return -1;

            if (inIndex >= 0 && inIndex < playlists.Count && playlists.Count > 0) return inIndex;

            return inIndex < 0 ? 0 : playlists.Count - 1;
        }

        public static void Load()
        {
            instance = new Library();
        }

        private void LoadNonStatic()
        {
            try
            {
                Task<SaveClass> loadXmlTask = SaveClass.Load();
                Task<Tuple<int, double>> loadIndexMilliseconds = LoadIndexMilliseconds();

                Tuple<int, double> indexMilliseconds = loadIndexMilliseconds.Result;
                SaveClass sc = loadXmlTask.Result;

                try
                {
                    foreach (SavePlaylist savePlaylist in sc.Playlists)
                    {
                        _playlists.Add(new Playlist(savePlaylist));
                    }
                }
                catch (Exception e) { }

                try
                {
                    HaveToLoad = sc.CurrentPlaylistIndex == -2;
                    CurrentPlaylistIndex = HaveToLoad ? 0 : sc.CurrentPlaylistIndex;

                    if (CurrentPlaylist != null)
                    {
                        CurrentPlaylist.CurrentSongIndex = indexMilliseconds.Item1;
                        CurrentPlaylist.SongPositionMilliseconds = indexMilliseconds.Item2;
                    }
                }
                catch (Exception e) { }
            }
            catch (Exception e) { }
        }

        private async Task<Tuple<int, double>> LoadIndexMilliseconds()
        {
            try
            {
                int index;
                double milliseconds;
                string text;
                string[] parts;
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("CurrentSongIndex.txt");

                text = await PathIO.ReadTextAsync(file.Path);
                parts = text.Split(';');

                if (int.TryParse(parts[0], out index) && double.TryParse(parts[1], out milliseconds))
                {
                    return new Tuple<int, double>(index, milliseconds);
                }
            }
            catch { }

            return new Tuple<int, double>(0, 0);
        }

        public async void LoadPlaylistsFromStorage()
        {
            List<Playlist> list = new List<Playlist>();
            list.AddRange(await LoadPlaylistsFromStorage(KnownFolders.MusicLibrary));

            foreach (Playlist playlist in list)
            {
                await playlist.LoadSongsFromStorage();
            }

            playlists = new List<Playlist>(list);
            DeleteEmptyPlaylists();

            currentPlaylistIndex = 0;
            CurrentPlaylist.CurrentSongIndex = 0;
            Save();

            Loaded();
        }

        public event LoadedEventHandler Loaded;

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
            catch (Exception e) { }

            return list;
        }

        public static void Save()
        {
            if (IsEmpty()) return;
            saveAgain = true;

            if (!isSaveing)
            {
                while (saveAgain)
                {
                    saveAgain = false;
                    Current.SaveNonStatic();
                }

                isSaveing = false;
            }
        }

        public static async void SaveAsync()
        {
            Save();
        }

        private void SaveNonStatic()
        {
            SaveClass sc = new SaveClass(currentPlaylistIndex, playlists);
            sc.Save();
        }

        public static async void SavePlayCommand(bool command)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + playCommandFileName;

            try
            {
                await PathIO.WriteTextAsync(path, command.ToString());
            }
            catch (FileNotFoundException e)
            {
                try
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(playCommandFileName);
                    await PathIO.WriteTextAsync(path, command.ToString());
                }
                catch { }
            }
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

        private bool HaveCurrentPlaylist()
        {
            return currentPlaylistIndex >= 0 && currentPlaylistIndex < playlists.Count;
        }

        public List<Playlist> GetPlaylists()
        {
            return playlists;
        }

        public static async void SaveSongIndexAndMilliseconds(int index, double milliseconds)
        {
            string text = index.ToString() + ";" + milliseconds.ToString();
            StorageFile file = await ApplicationData.Current.LocalFolder.
                CreateFileAsync(currentSongIndexFileName, CreationCollisionOption.ReplaceExisting);

            await PathIO.WriteTextAsync(file.Path, text);
        }

        public static async void DeleteSongIndexAndMilliseconds()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(currentSongIndexFileName);

                await file.DeleteAsync();
            }
            catch { }
        }

        public static bool IsEmpty()
        {
            return Current._playlists.Count == 0;
        }

        public void RemoveSongFromCurrentPlaylist(int index)
        {
            CurrentPlaylist.RemoveSong(index);
            DeleteEmptyPlaylists();
        }

        public void RemoveSongFromCurrentPlaylist(Song song)
        {
            CurrentPlaylist.RemoveSong(song);
            DeleteEmptyPlaylists();
        }

        private void DeleteEmptyPlaylists()
        {
            for (int i = playlists.Count - 1; i >= 0; i--)
            {
                if (playlists[i].IsEmpty()) DeleteAt(i);
            }
        }

        public void Delete(Playlist playlist)
        {
            if (IsEmpty()) return;

            Playlist oldCurrentPlaylist = CurrentPlaylist;
            _playlists.Remove(playlist);
            _playlists = new List<Playlist>(_playlists);

            SetPlaylistToCurrentPlaylist(oldCurrentPlaylist);
        }

        public void DeleteAt(int index)
        {
            if (IsEmpty()) return;

            Playlist oldCurrentPlaylist = CurrentPlaylist;
            _playlists.RemoveAt(index);
            _playlists = new List<Playlist>(_playlists);

            SetPlaylistToCurrentPlaylist(oldCurrentPlaylist);
        }

        private void SetPlaylistToCurrentPlaylist(Playlist playlist)
        {
            int newCurrentPlaylistIndex = _playlists.IndexOf(playlist);

            if (newCurrentPlaylistIndex != -1 && CurrentPlaylistIndex != newCurrentPlaylistIndex)
            {
                CurrentPlaylistIndex = newCurrentPlaylistIndex;
            }
            else if(newCurrentPlaylistIndex ==-1)
            {
                CurrentPlaylistIndex = currentPlaylistIndex;
            }
        }

        public async void RefreshPlaylist(int index)
        {
            if (IsEmpty()) return;

            await playlists[index].LoadSongsFromStorage();
            DeleteEmptyPlaylists();
        }

        public async void SearchForNewPlaylists()
        {
            bool existsAllready, reachedIndex = false;
            int newCurrentplaylistIndex = CurrentPlaylistIndex;
            string currentPlaylistAbsolutePath = CurrentPlaylist.AbsolutePath;

            List<Playlist> possiblePlaylists = new List<Playlist>(await LoadPlaylistsFromStorage(KnownFolders.MusicLibrary));
            List<Playlist> exceptPlaylist = new List<Playlist>();

            for (int i = 0; i < possiblePlaylists.Count; i++)
            {
                existsAllready = false;

                foreach (Playlist playlist in playlists)
                {
                    if (possiblePlaylists[i].AbsolutePath == playlist.AbsolutePath)
                    {
                        exceptPlaylist.Add(playlist);
                        existsAllready = true;

                        if (playlist.AbsolutePath == currentPlaylistAbsolutePath) reachedIndex = true;
                        break;
                    }
                }

                if (!existsAllready)
                {
                    await possiblePlaylists[i].LoadSongsFromStorage();

                    if (!possiblePlaylists[i].IsEmpty())
                    {
                        newCurrentplaylistIndex += Convert.ToInt32(!reachedIndex);
                        exceptPlaylist.Add(possiblePlaylists[i]);
                    }
                }
            }

            _playlists = exceptPlaylist;
            CurrentPlaylistIndex = newCurrentplaylistIndex;

            Save();
            Loaded();
        }
    }
}
