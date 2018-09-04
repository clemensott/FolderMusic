using FolderMusicLib;
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

        private volatile bool isForeground, cancelLoading = false;
        private static bool loaded;
        private static string currentSongMillisecondsFileName = "CurrentSongMilliseconds.txt",
          currentSongFileName = "currentSong.xml", skipSongsFileName = "SkipSongs.xml", playCommandFileName = "PlayCommand.txt";

        private int currentPlaylistIndex = 0;
        private double currentSongPositionMilliseconds;
        private Song currentSong = new Song();
        private List<Playlist> playlists;

        public bool CanceledLoading { get { return cancelLoading; } }

        public bool IsForeground { get { return isForeground; } }

        public bool IsEmpty { get { return playlists.Count == 0; ; } }

        public static bool IsLoaded { get { return loaded; } }

        public static Library Current
        {
            get
            {
                if (instance == null) instance = new Library();

                return instance;
            }
        }

        public List<Playlist> Playlists
        {
            get
            {
                return IsLoaded && !IsEmpty ? playlists : 
                    new List<Playlist>() { new Playlist(currentSong, currentSongPositionMilliseconds) };
            }
            set
            {
                if (playlists == value) return;

                playlists = value;

                if (isForeground)
                {
                    BackgroundCommunicator.SendLoadXML(GetXmlText());
                    ViewModel.Current.UpdatePlaylistsAndIndex();
                }
            }
        }

        public Playlist this[int index]
        {
            get { return Playlists[index]; }
            set { Playlists[index] = value; }
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

                if (isForeground)
                {
                    BackgroundCommunicator.SendCurrentPlaylistIndex();

                    ViewModel.Current.SetScrollLbxCurrentPlaylist();
                    ViewModel.Current.UpdateCurrentPlaylistIndexAndRest();
                }
            }
        }

        public Playlist CurrentPlaylist
        {
            get { return this[CurrentPlaylistIndex]; }
            set
            {
                if (IsEmpty) return;
                int index = value.PlaylistIndex;

                if (index != -1) CurrentPlaylistIndex = index;
            }
        }

        private Library()
        {
            playlists = new List<Playlist>();
        }

        public void SetIsForeground()
        {
            isForeground = true;
            BackgroundCommunicator.SetReceivedEvent();
            BackgroundCommunicator.SendGetXmlText();
        }

        private int GetPossibleCurrentPlaylistIndex(int inIndex)
        {
            int playlistsCount = Playlists.Count;

            if (playlistsCount == 0) return -1;

            if (inIndex >= 0 && inIndex < playlistsCount && playlistsCount > 0) return inIndex;

            return inIndex < 0 ? 0 : playlistsCount - 1;
        }

        public void Load(string xmlText)
        {
            SaveClass sc = XmlConverter.Deserialize<SaveClass>(xmlText);

            playlists = sc.Playlists;
            currentPlaylistIndex = sc.CurrentPlaylistIndex;

            loaded = true;

            if (isForeground)
            {
                ViewModel.Current.UpdatePlaylistsAndIndex();
                ViewModel.Current.UpdateCurrentPlaylistIndexAndRest();
            }
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
            playlists = sc.Playlists;

            loaded = true;
            currentPlaylistIndex = sc.CurrentPlaylistIndex == -2 ? 0 : sc.CurrentPlaylistIndex;
        }

        private async Task LoadCurrentSongMilliseconds()
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(currentSongMillisecondsFileName);
                string text = await PathIO.ReadTextAsync(file.Path);

                currentSongPositionMilliseconds = double.Parse(text);
            }
            catch { }
        }

        public async Task LoadPlaylistsFromStorage()
        {
            cancelLoading = false;
            ViewModel.Current.Pause();

            await DeleteCurrentSongMillisecondsFile();
            await DeleteCurrentSongFile();
            List<Playlist> list = new List<Playlist>(await LoadPlaylistsFromStorage(KnownFolders.MusicLibrary));

            foreach (Playlist playlist in list)
            {
                await playlist.LoadSongsFromStorage();

                if (CanceledLoading) return;
            }

            Playlists = new List<Playlist>(list);
            DeleteEmptyPlaylists();

            CurrentPlaylistIndex = 0;

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
            SaveClass sc = new SaveClass(CurrentPlaylistIndex, playlists as List<Playlist>);

            return XmlConverter.Serialize(sc);
        }

        public async Task SaveAsync()
        {
            SaveClass sc = new SaveClass(CurrentPlaylistIndex, playlists as List<Playlist>);

            await sc.Save();
        }

        public static async Task SavePlayCommand(bool play)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + playCommandFileName;

            try
            {
                await PathIO.WriteTextAsync(path, play.ToString());
            }
            catch (FileNotFoundException)
            {
                try
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(playCommandFileName);
                    await PathIO.WriteTextAsync(path, play.ToString());
                }
                catch { }
            }
            catch { }
        }

        public static async Task<bool> LoadPlayCommand()
        {
            string text, path = ApplicationData.Current.LocalFolder.Path + "\\" + playCommandFileName;

            try
            {
                text = await PathIO.ReadTextAsync(path);
                return bool.Parse(text);
            }
            catch { }

            return false;
        }

        public int GetPlaylistIndex(Playlist playlist)
        {
            return Playlists.IndexOf(playlist);
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
            if (IsEmpty || !playlists.Contains(playlist)) return;

            Playlist oldCurrentPlaylist = CurrentPlaylist;
            playlists.Remove(playlist);
            playlists = new List<Playlist>(playlists);

            SetPlaylistToCurrentPlaylist(oldCurrentPlaylist);

            if (isForeground)
            {
                BackgroundCommunicator.SendRemovePlaylist(playlist);
                ViewModel.Current.UpdatePlaylistsAndIndex();
            }
        }

        public void DeleteAt(int index)
        {
            Delete(this[index]);
        }

        public int GetPlaylistIndexWhichContainsSong(Song song)
        {
            var playlistsWithSong = playlists.Where(x => x.Songs.Contains(song)).ToList();

            if (playlistsWithSong.Count != 1) return -1;
            return playlists.IndexOf(playlistsWithSong[0]);
        }

        private void SetPlaylistToCurrentPlaylist(Playlist playlist)
        {
            int newCurrentPlaylistIndex = playlists.IndexOf(playlist);

            if (newCurrentPlaylistIndex != -1 && CurrentPlaylistIndex != newCurrentPlaylistIndex)
            {
                CurrentPlaylistIndex = newCurrentPlaylistIndex;
            }
        }

        public async Task SearchForNewPlaylists()
        {
            int updatedCurrentPlaylistIndex = 0;
            string currentPlaylistAbsolutePath = CurrentPlaylist.AbsolutePath;

            cancelLoading = false;

            List<Playlist> possiblePlaylists = new List<Playlist>(await LoadPlaylistsFromStorage(KnownFolders.MusicLibrary));
            List<Playlist> updatedPlaylists = new List<Playlist>();

            foreach (Playlist possiblePlaylist in possiblePlaylists)
            {
                updatedPlaylists = await GetUpdatedPlaylistsWithAddedPlaylistIfNotContains(possiblePlaylist, updatedPlaylists);

                if (CanceledLoading) return;

                if (possiblePlaylist.AbsolutePath == currentPlaylistAbsolutePath)
                {
                    updatedCurrentPlaylistIndex = updatedPlaylists.Count - 1;
                }
            }

            Playlists = updatedPlaylists;
            CurrentPlaylistIndex = updatedCurrentPlaylistIndex;
        }

        private async Task<List<Playlist>> GetUpdatedPlaylistsWithAddedPlaylistIfNotContains
            (Playlist possiblePlaylist, List<Playlist> updatedPlaylists)
        {
            bool existsAllready = false;

            foreach (Playlist playlist in Playlists)
            {
                if (CanceledLoading) return updatedPlaylists;

                if (possiblePlaylist.AbsolutePath == playlist.AbsolutePath)
                {
                    updatedPlaylists.Add(playlist);
                    existsAllready = true;
                    break;
                }
            }

            if (!existsAllready)
            {
                await possiblePlaylist.LoadSongsFromStorage();

                if (!possiblePlaylist.IsEmptyOrLoading)
                {
                    updatedPlaylists.Add(possiblePlaylist);
                }
            }

            return updatedPlaylists;
        }

        public void CancelLoading()
        {
            cancelLoading = true;
        }
    }
}
