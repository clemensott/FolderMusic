using FolderMusicLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;

namespace LibraryLib
{
    public delegate void ScrollEventHandler(object sender, Playlist playlist);

    public class Library
    {
        public event ScrollEventHandler ScrollToIndex;
        private static Library instance;

        private volatile bool isForeground, cancelLoading = false;
        private static bool loaded;

        private int currentPlaylistIndex = 0;
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
            get { return IsLoaded && !IsEmpty ? playlists : new List<Playlist>() { new Playlist(CurrentSong.Current) }; }
            set
            {
                if (playlists == value) return;

                playlists = value;

                if (isForeground)
                {
                    BackgroundCommunicator.SendLoadXML();

                    SetLoaded();
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
                if (currentPlaylistIndex == value || value == -1) return;
                if (CurrentPlaylistIndex == value)
                {
                    currentPlaylistIndex = value;

                    if (isForeground) BackgroundCommunicator.SendCurrentPlaylistIndex();
                    return;
                }

                TimeSpan position = BackgroundMediaPlayer.Current.Position;
                TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;

                CurrentPlaylist.SongPositionPercent = position.TotalMilliseconds / duration.TotalMilliseconds;
                currentPlaylistIndex = value;

                if (isForeground)
                {
                    BackgroundCommunicator.SendCurrentPlaylistIndex();
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

        public void UpdatePlaylistsObjectANdCurrentPlaylistSongsObject()
        {
            playlists = new List<Playlist>(playlists);
            CurrentPlaylist.UpdateSongsObject();
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

        public int GetPlaylistIndex(Playlist playlist)
        {
            return Playlists.IndexOf(playlist);
        }

        public bool HavePlaylistIndex(string playlistAbsolutePath, out int playlistIndex)
        {
            Playlist[] playlists = Playlists.Where(x => x.AbsolutePath == playlistAbsolutePath).ToArray();
            playlistIndex = -1;

            if (playlists.Length != 1) return false;

            playlistIndex = Playlists.IndexOf(playlists[0]);
            return true;
        }

        public bool HavePlaylistIndexAndSongsIndex(string songPath, out int playlistIndex, out int songsIndex)
        {
            for (playlistIndex = 0; playlistIndex < Length; playlistIndex++)
            {
                Song[] songs = Library.Current[playlistIndex].Songs.Where(x => x.Path == songPath).ToArray();

                if (songs.Length == 1)
                {
                    songsIndex = Playlists[playlistIndex].Songs.IndexOf(songs[0]);
                    return true;
                }
            }

            playlistIndex = -1;
            songsIndex = -1;

            return false;
        }

        public bool HavePlaylistIndexAndSongsIndex(Song song, out int playlistIndex, out int songsIndex)
        {
            for (playlistIndex = 0; playlistIndex < Length; playlistIndex++)
            {
                songsIndex = Playlists[playlistIndex].Songs.IndexOf(song);

                if (songsIndex != -1) return true;
            }

            playlistIndex = -1;
            songsIndex = -1;

            return false;
        }

        public void SetLoaded()
        {
            loaded = true;

            if (isForeground)
            {
                ViewModel.Current.UpdatePlaylists();
                ViewModel.Current.UpdateCurrentPlaylistIndexAndRest();
            }
        }

        public void Load(string xmlText)
        {
            SaveLibray sc = XmlConverter.Deserialize<SaveLibray>(xmlText);

            playlists = sc.Playlists;
            currentPlaylistIndex = sc.CurrentPlaylistIndex;

            SetLoaded();
        }

        public void Load()
        {
            CurrentSong.Current.Load();
            LoadLibrary();

            SetCurrentSong();
            CurrentPlaylist.SongPositionPercent = CurrentSong.Current.PositionPercent;
        }

        private void SetCurrentSong()
        {
            Song[] songs = CurrentPlaylist.Songs.Where(x => x.Path == CurrentSong.Current.Song.Path).ToArray();

            if (songs.Length != 1) return;

            CurrentPlaylist.SongsIndex = CurrentPlaylist.Songs.IndexOf(songs[0]);
        }

        private void LoadLibrary()
        {
            SaveLibray sc = LoadSaveLibrary();
            playlists = sc.Playlists;

            currentPlaylistIndex = sc.CurrentPlaylistIndex == -2 ? 0 : sc.CurrentPlaylistIndex;
            loaded = true;
        }

        private SaveLibray LoadSaveLibrary()
        {
            for (int i = 0; i < 2; i++)
            {
                SaveLibray saveLib = SaveLibray.Load();

                if (saveLib != null) return saveLib;

                FolderMusicDebug.SaveTextClass.Current.SaveText("Coundn't load Data");
            }

            for (int i = 0; i < 2; i++)
            {
                SaveLibray saveLib = SaveLibray.LoadBackup();

                if (saveLib != null) return saveLib;

                FolderMusicDebug.SaveTextClass.Current.SaveText("Coundn't load Backup");
            }

            return new SaveLibray(-2, new List<Playlist>());
        }

        public async Task ResetLibraryFromStorage()
        {
            cancelLoading = false;
            ViewModel.Current.Pause();

            List<Task> tasks = new List<Task>();
            List<Playlist> list = new List<Playlist>(await LoadPlaylistsFromStorage());

            foreach (Playlist playlist in list)
            {
                await playlist.LoadSongsFromStorage();

                if (CanceledLoading) return;
            }

            foreach (Task task in tasks) await task;

            CurrentPlaylistIndex = 0;

            DeleteEmptyPlaylists(ref list);
            Playlists = new List<Playlist>(list);
        }

        public async Task UpdateExistingPlaylists()
        {
            for (int i = Length - 1; i >= 0; i--)
            {
                if (CanceledLoading) return;

                await Playlists[i].UpdateSongsFromStorage();
            }
        }

        public async Task AddNotExistingPlaylists()
        {
            int updatedCurrentPlaylistIndex = 0;
            string currentPlaylistAbsolutePath = CurrentPlaylist.AbsolutePath;

            cancelLoading = false;

            List<Playlist> possiblePlaylists = new List<Playlist>(await LoadPlaylistsFromStorage());
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

            CurrentPlaylistIndex = updatedCurrentPlaylistIndex;
            Playlists = updatedPlaylists;
        }

        private async Task<List<Playlist>> GetUpdatedPlaylistsWithAddedPlaylistIfNotContains
            (Playlist possiblePlaylist, List<Playlist> updatedPlaylists)
        {
            Playlist[] existPlaylists = Playlists.Where(x => x.AbsolutePath == possiblePlaylist.AbsolutePath).ToArray();

            if (existPlaylists.Length == 0)
            {
                await possiblePlaylist.LoadSongsFromStorage();

                if (!possiblePlaylist.IsEmptyOrLoading) updatedPlaylists.Add(possiblePlaylist);
            }
            else updatedPlaylists.Add(existPlaylists[0]);

            return updatedPlaylists;
        }

        private async Task<List<Playlist>> LoadPlaylistsFromStorage()
        {
            return await LoadPlaylistsFromStorage(KnownFolders.MusicLibrary);
        }

        private async Task<List<Playlist>> LoadPlaylistsFromStorage(StorageFolder folder)
        {
            List<Playlist> list = new List<Playlist>();

            try
            {
                list.Add(new Playlist(folder.Path));
                //return list;

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
            SaveLibray sc = new SaveLibray(CurrentPlaylistIndex, playlists as List<Playlist>);

            return XmlConverter.Serialize(sc);
        }

        public async Task SaveAsync()
        {
            Save();
        }

        public void Save()
        {
            SaveLibray sc = new SaveLibray(CurrentPlaylistIndex, playlists as List<Playlist>);

            sc.Save();
        }

        private void DeleteEmptyPlaylists(ref List<Playlist> playlists)
        {
            for (int i = playlists.Count - 1; i >= 0; i--)
            {
                if (playlists[i].IsEmptyOrLoading) playlists.RemoveAt(i);
            }
        }

        public void DeleteEmptyPlaylists()
        {
            for (int i = playlists.Count - 1; i >= 0; i--)
            {
                if (playlists[i].IsEmptyOrLoading) DeleteAt(i);
            }
        }

        public void Delete(Playlist playlist)
        {
            if (IsEmpty || playlist.PlaylistIndex == -1) return;

            BackgroundCommunicator.SendRemovePlaylist(playlist);

            Playlist oldCurrentPlaylist = CurrentPlaylist;
            playlists.Remove(playlist);
            playlists = new List<Playlist>(playlists);

            SetPlaylistToCurrentPlaylist(oldCurrentPlaylist);

            if (isForeground)
            {
                ViewModel.Current.UpdatePlaylists();
                ViewModel.Current.UpdateCurrentPlaylistIndexAndRest();

                if (IsEmpty)
                {
                    CurrentSong.Current.Unset();
                    SaveLibray.Delete();
                }
            }
        }

        public void DeleteAt(int index)
        {
            Delete(this[index]);
        }

        private void SetPlaylistToCurrentPlaylist(Playlist playlist)
        {
            int newCurrentPlaylistIndex = playlists.IndexOf(playlist);

            if (newCurrentPlaylistIndex != -1 && CurrentPlaylistIndex != newCurrentPlaylistIndex)
            {
                CurrentPlaylistIndex = newCurrentPlaylistIndex;
            }
            else CurrentPlaylistIndex = CurrentPlaylistIndex;
        }

        public void FireScrollEvent(Playlist playlist)
        {
            if (ScrollToIndex == null || playlist.PlaylistIndex == -1) return;

            ScrollToIndex(this, playlist);
        }

        public void CancelLoading()
        {
            cancelLoading = true;
        }
    }
}
