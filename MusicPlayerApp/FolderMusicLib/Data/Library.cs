using MusicPlayer.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;

namespace MusicPlayer.Data
{
    public class Library : LibraryBase
    {
        internal static Library dataInstance;
        internal static LibraryBase currentInstance, nonLoadedInstance;

        public static ILibrary Current
        {
            get
            {
                if (currentInstance == null) nonLoadedInstance = currentInstance = NonLoadedLibrary.Current;

                return currentInstance;
            }
        }

        internal static LibraryBase Base
        {
            get
            {
                if (currentInstance == null) nonLoadedInstance = currentInstance = NonLoadedLibrary.Current;

                return currentInstance;
            }
        }

        internal static Library Data
        {
            get
            {
                return dataInstance;
            }
        }

        public static Feedback Events
        {
            get
            {
                return Feedback.Current;
            }
        }

        public static bool IsLoaded() { return Current != nonLoadedInstance; }

        public static bool IsLoaded(ILibrary library)
        {
            return IsLoaded() && Current == library;
        }

        public static bool IsLoaded(PlaylistList playlists)
        {
            return IsLoaded() && Current.Playlists == playlists;
        }

        public static bool IsLoaded(Playlist playlist)
        {
            return IsLoaded() && Current.Playlists.Any(p => ReferenceEquals(p, playlist));
        }

        public static bool IsLoaded(SongList songs)
        {
            return IsLoaded() && Current.Playlists.Any(p => p.Songs == songs);
        }

        public static bool IsLoaded(Song song)
        {
            return IsLoaded() && Current.Playlists.Any(p => p.Songs.Any(s => ReferenceEquals(s, song)));
        }


        internal Library(IEnumerable<Playlist> playlists, int currentPlaylistIndex)
        {
            this.playlists = new PlaylistList(playlists);
            this.currentPlaylistIndex = currentPlaylistIndex;

            SetCurrentSong();

        }

        private void SetCurrentSong()
        {
            string currentPlaySongPath = (CurrentPlaySong.Current.Song ?? new Song()).Path;
            Song currentSong = playlists[currentPlaylistIndex].Songs.FirstOrDefault(x => x.Path == currentPlaySongPath);

            if (currentSong == null) return;

            playlists[currentPlaylistIndex].SongsIndex = playlists[currentPlaylistIndex].Songs.IndexOf(currentSong);
        }

        public static void Load(bool isForeground)
        {
            LibraryBase.isForeground = isForeground;

            if (isForeground)
            {
                BackForegroundCommunicator.StartCommunication(isForeground);

                //AskForLibraryData();
            }
            else
            {
                ILibrary oldLibrary = Current;

                CurrentPlaySong.Current.Load();

                currentInstance = dataInstance = LoadLibrary();

                BackForegroundCommunicator.StartCommunication(isForeground);
                Feedback.Current.RaiseLibraryChanged(oldLibrary, Current);
            }
        }

        private static Library LoadLibrary()
        {
            SaveLibray sc = LoadSaveLibrary();

            return new Library(sc.Playlists, sc.CurrentPlaylistIndex);
        }

        private static SaveLibray LoadSaveLibrary()
        {
            for (int i = 0; i < 2; i++)
            {
                SaveLibray saveLib = SaveLibray.Load();

                if (saveLib != null) return saveLib;

                FolderMusicDebug.DebugEvent.SaveText("Coundn't load Data");
            }

            for (int i = 0; i < 2; i++)
            {
                SaveLibray saveLib = SaveLibray.LoadBackup();

                if (saveLib != null) return saveLib;

                FolderMusicDebug.DebugEvent.SaveText("Coundn't load Backup");
            }

            return new SaveLibray(0, new List<Playlist>() { new Playlist() });
        }

        internal static void Load(string xmlText)
        {
            nonLoadedInstance = currentInstance;
            SaveLibray sc = XmlConverter.Deserialize<SaveLibray>(xmlText);

            currentInstance = dataInstance = new Library(sc.Playlists, sc.CurrentPlaylistIndex);

            Feedback.Current.RaiseLibraryChanged(nonLoadedInstance, currentInstance);
        }

        internal void UpdateAddPlaylist(int index, Playlist addPlaylist, Playlist currentPlaylist)
        {
            int newCurrentPlaylistIndex = Playlists.IndexOf(currentPlaylist);

            if (newCurrentPlaylistIndex != -1) currentPlaylistIndex = newCurrentPlaylistIndex;

            Feedback.Current.RaisePlaylistsPropertyChanged(new ChangedPlaylist[] { new ChangedPlaylist(index, addPlaylist) },
                  new ChangedPlaylist[0], currentPlaylist, CurrentPlaylist);
        }

        internal void UpdateRemovePlaylist(int index, Playlist removePlaylist, Playlist currentPlaylist)
        {
            int newCurrentPlaylistIndex = Playlists.IndexOf(currentPlaylist);

            if (newCurrentPlaylistIndex != -1) currentPlaylistIndex = newCurrentPlaylistIndex;

            Feedback.Current.RaisePlaylistsPropertyChanged(new ChangedPlaylist[0], new ChangedPlaylist[]
                { new ChangedPlaylist(index, removePlaylist) }, currentPlaylist, CurrentPlaylist);
        }

        internal void UpdateAddRemovePlaylist(int index, Playlist addPlaylist, Playlist removePlaylist, Playlist currentPlaylist)
        {
            int newCurrentPlaylistIndex = Playlists.IndexOf(currentPlaylist);

            if (newCurrentPlaylistIndex != -1) currentPlaylistIndex = newCurrentPlaylistIndex;

            Feedback.Current.RaisePlaylistsPropertyChanged(new ChangedPlaylist[] { new ChangedPlaylist(index, addPlaylist) },
                new ChangedPlaylist[] { new ChangedPlaylist(index, removePlaylist) }, currentPlaylist, CurrentPlaylist);
        }

        internal void SetPlaylists(PlaylistList playlists, Playlist currentPlaylist)
        {
            bool playlistsChanged = false, currentPlaylistChanged = false;
            PlaylistList oldPlaylists = Playlists;
            Playlist oldCurrentPlaylist = CurrentPlaylist;

            if (playlists != Playlists)
            {
                this.playlists = playlists;
                playlistsChanged = true;
            }
            else if (!playlists.SequenceEqual(Playlists)) playlistsChanged = true;

            if (currentPlaylist != CurrentPlaylist)
            {
                CurrentPlaylist.SongPositionPercent = BackgroundMediaPlayer.Current.Position.TotalMilliseconds /
                    BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds;

                currentPlaylistIndex = playlists.IndexOf(currentPlaylist);
                currentPlaylistChanged = true;
            }

            if (!IsLoaded()) return;

            SaveAsync();

            if (playlistsChanged)
            {
                Feedback.Current.RaisePlaylistsPropertyChanged(oldPlaylists, Playlists, oldCurrentPlaylist, CurrentPlaylist);
            }
            else if (currentPlaylistChanged)
            {
                Feedback.Current.RaiseCurrentPlaylistPropertyChanged(this, oldCurrentPlaylist, CurrentPlaylist);
            }
        }

        public override async Task RefreshLibraryFromStorage()
        {
            if (!IsLoaded()) return;

            Library oldLibrary = this;
            cancelLoading = false;
            IsPlaying = false;

            List<Task> tasks = new List<Task>();
            List<Playlist> list = new List<Playlist>(await LoadPlaylistsFromStorage());

            foreach (Playlist playlist in list)
            {
                await playlist.LoadSongsFromStorage();

                if (CanceledLoading) return;
            }

            foreach (Task task in tasks) await task;

            currentPlaylistIndex = 0;
            playlists = new PlaylistList(list.Where(p => !p.IsEmptyOrLoading));

            Feedback.Current.RaiseLibraryChanged(oldLibrary, this);
        }

        public override async Task UpdateExistingPlaylists()
        {
            for (int i = Length - 1; i >= 0; i--)
            {
                if (CanceledLoading) return;

                await Playlists[i].UpdateSongsFromStorage();
            }
        }

        public override async Task AddNotExistingPlaylists()
        {
            Playlist currentPlaylist = CurrentPlaylist;
            string currentPlaylistAbsolutePath = CurrentPlaylist.AbsolutePath;

            cancelLoading = false;

            List<Playlist> possiblePlaylists = new List<Playlist>(await LoadPlaylistsFromStorage());
            List<Playlist> updatedPlaylists = new List<Playlist>();

            foreach (Playlist possiblePlaylist in possiblePlaylists)
            {
                updatedPlaylists = await GetUpdatedPlaylistsWithAddedPlaylistIfNotContains(possiblePlaylist, updatedPlaylists);

                if (CanceledLoading) return;
            }

            SetPlaylists(new PlaylistList(updatedPlaylists), currentPlaylist);
        }

        private async Task<List<Playlist>> GetUpdatedPlaylistsWithAddedPlaylistIfNotContains
            (Playlist possiblePlaylist, List<Playlist> updatedPlaylists)
        {
            Playlist existingPlaylist = Playlists.FirstOrDefault(x => x.AbsolutePath == possiblePlaylist.AbsolutePath);

            if (existingPlaylist == null)
            {
                await possiblePlaylist.LoadSongsFromStorage();

                if (possiblePlaylist.Songs.Count > 0) updatedPlaylists.Add(possiblePlaylist);
            }
            else updatedPlaylists.Add(existingPlaylist);

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

                var folders = await folder.GetFoldersAsync();

                foreach (StorageFolder listFolder in folders)
                {
                    list.AddRange(await LoadPlaylistsFromStorage(listFolder));
                }
            }
            catch { }

            return list;
        }

        internal override string GetXmlText()
        {
            SaveLibray sc = new SaveLibray(CurrentPlaylistIndex, playlists.ToList());

            return XmlConverter.Serialize(sc);
        }

        public override async Task SaveAsync()
        {
            if (isForeground == false) Save();
        }

        public override void Save()
        {
            SaveLibray sc = new SaveLibray(CurrentPlaylistIndex, playlists.ToList());

            sc.Save();
        }

        public override void CancelLoading()
        {
            cancelLoading = true;
        }

        protected override bool GetIsPlaying()
        {
            return isPlayling;
        }

        protected override void SetIsPlaying(bool value)
        {
            if (value == isPlayling) return;

            isPlayling = value;

            Feedback.Current.RaisePlayStateChanged(this, value);
        }

        protected override PlaylistList GetPlaylists()
        {
            return playlists;
        }

        protected override void SetPlaylists(PlaylistList newPlaylists)
        {
            if (playlists == newPlaylists) return;

            if (!IsLoaded()) playlists = newPlaylists;
            else SetPlaylists(newPlaylists, CurrentPlaylist);
        }

        protected override int GetCurrentPlaylistIndex()
        {
            return currentPlaylistIndex;
        }

        protected override void SetCurrentPlaylistIndex(int newCurrentPlaylistIndex)
        {
            if (currentPlaylistIndex == newCurrentPlaylistIndex) return;

            if (!IsLoaded()) currentPlaylistIndex = newCurrentPlaylistIndex;
            else SetPlaylists(Playlists, Playlists[newCurrentPlaylistIndex]);
        }

        protected override void SetCurrentPlaylist(Playlist newCurrentPlaylist)
        {
            if (!IsLoaded() || newCurrentPlaylist == CurrentPlaylist) return;
            else SetPlaylists(Playlists, newCurrentPlaylist);
        }
    }
}
