using MusicPlayer.Communication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Windows.Media.Playback;
using Windows.Storage;

namespace MusicPlayer.Data
{
    public class Library : ILibrary
    {
        public const double DefaultSongsPosition = 0, DefaultSongsPositionMillis = 1;

        private bool isPlaying;
        private MediaPlayerState playerState;
        private IPlaylist currentPlaylist;
        private IPlaylistCollection playlists;
        private BackForegroundCommunicator communicator;

        public event EventHandler<IsPlayingChangedEventArgs> IsPlayingChanged;
        public event EventHandler<PlayerStateChangedEventArgs> PlayerStateChanged;
        public event EventHandler<PlaylistsChangedEventArgs> PlaylistsChanged;
        public event EventHandler<CurrentPlaylistChangedEventArgs> CurrentPlaylistChanged;
        public event EventHandler SettingsChanged;
        public event EventHandler Loaded;

        public IPlaylist this[int index] { get { return Playlists.ElementAtOrDefault(index); } }

        public bool IsForeground { get; private set; }

        public bool IsLoaded { get; private set; }

        public bool IsPlaying
        {
            get { return isPlaying; }
            set
            {
                if (value == isPlaying) return;

                isPlaying = value;
                IsPlayingChangedEventArgs args = new IsPlayingChangedEventArgs(value);
                IsPlayingChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(IsPlaying));
            }
        }

        public MediaPlayerState PlayerState
        {
            get { return playerState; }
            set
            {
                MobileDebug.Service.WriteEvent("SetPlayerState", playerState, value);
                if (value == playerState) return;

                PlayerStateChangedEventArgs args = new PlayerStateChangedEventArgs(playerState, value);
                playerState = value;
                PlayerStateChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(PlayerState));
            }
        }

        public IPlaylist CurrentPlaylist
        {
            get { return currentPlaylist; }
            set
            {
                if (value == currentPlaylist) return;

                try
                {
                    if (!IsForeground && currentPlaylist != null)
                    {
                        TimeSpan position = BackgroundMediaPlayer.Current.Position;
                        TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;
                        currentPlaylist.CurrentSongPosition = position.TotalMilliseconds / duration.TotalMilliseconds;
                    }
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("SetCurrentPlaylistSaveOldPositionFail", e, currentPlaylist?.AbsolutePath);
                }

                CurrentPlaylistChangedEventArgs args = new CurrentPlaylistChangedEventArgs(currentPlaylist, value);
                currentPlaylist = value;
                CurrentPlaylistChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(CurrentPlaylist));
            }
        }

        public IPlaylistCollection Playlists
        {
            get { return playlists; }
            set
            {
                if (value == playlists) return;

                PlaylistsChangedEventArgs args = new PlaylistsChangedEventArgs(playlists, value);
                playlists = value;
                playlists.Parent = this;
                PlaylistsChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(Playlists));
            }
        }

        public SkipSongs SkippedSongs { get; private set; }

        internal Library(bool isForeground)
        {
            IsForeground = isForeground;
            IsLoaded = false;

            SkippedSongs = new SkipSongs(this);
            Playlists = new PlaylistCollection();
            CurrentPlaylist = null;
        }

        internal Library(CurrentPlaySong currentPlaySong)
        {
            IsForeground = false;
            IsLoaded = false;

            SkippedSongs = new SkipSongs(this);
            Playlists = new PlaylistCollection(currentPlaySong);
            CurrentPlaylist = Playlists.First();
        }

        public void BeginCommunication()
        {
            communicator = new BackForegroundCommunicator(this);
        }

        public void Load(IEnumerable<IPlaylist> playlists)
        {
            if (IsLoaded || playlists == null) return;

            string currentSongPath = CurrentPlaylist?.CurrentSong?.Path;
            double currentSongPosition = CurrentPlaylist?.CurrentSongPosition ?? 0;
            List<IPlaylist> remainingPlaylists = Playlists.ToList();
            List<IPlaylist> addPlaylists = new List<IPlaylist>();

            foreach (IPlaylist setPlaylist in playlists)
            {
                IPlaylist existingPlaylist = remainingPlaylists.FirstOrDefault(p => p.AbsolutePath == setPlaylist.AbsolutePath);

                if (existingPlaylist == null) addPlaylists.Add(setPlaylist);
                else
                {
                    existingPlaylist.Songs = setPlaylist.Songs;
                    remainingPlaylists.Remove(existingPlaylist);
                }
            }

            Playlists.Change(remainingPlaylists, addPlaylists);

            SetCurrentPlaylistAndCurrentSong(currentSongPath, currentSongPosition);

            //AutoSaveLoad.CheckLibrary(this, "LoadedComplete");
            IsLoaded = true;
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        private void SetCurrentPlaylistAndCurrentSong(string currentSongPath, double currentSongPosition)
        {
            foreach (IPlaylist playlist in Playlists)
            {
                Song currentSong = playlist.Songs.FirstOrDefault(s => s.Path == currentSongPath);

                if (currentSong != null)
                {
                    CurrentPlaylist = playlist;
                    CurrentPlaylist.CurrentSong = currentSong;
                    CurrentPlaylist.CurrentSongPosition = currentSongPosition;
                    return;
                }
            }

            CurrentPlaylist = Playlists.FirstOrDefault();
            if (CurrentPlaylist != null) CurrentPlaylist.CurrentSong = CurrentPlaylist.Songs.FirstOrDefault();
        }

        public async Task Reset(StopOperationToken stopToken)
        {
            IsPlaying = false;

            List<IPlaylist> refreshedPlaylists = new List<IPlaylist>();
            List<StorageFolder> folders = await GetStorageFolders();

            foreach (StorageFolder folder in await GetStorageFolders())
            {
                IPlaylist playlist = new Playlist(folder.Path);
                playlist.Parent = Playlists;

                await playlist.Reset(stopToken);

                if (stopToken.IsStopped) return;
                if (playlist.Songs.Count > 0) refreshedPlaylists.Add(playlist);
            }

            IPlaylistCollection playlists = new PlaylistCollection();
            playlists.Change(null, refreshedPlaylists);

            if (stopToken.IsStopped) return;

            Playlists = playlists;
            CurrentPlaylist = playlists.FirstOrDefault();
        }

        public async Task Update(StopOperationToken stopToken)
        {
            foreach (IPlaylist playlist in Playlists.ToArray())
            {
                if (stopToken.IsStopped) return;

                await playlist.Update(stopToken);
            }
        }

        public async Task ResetSongs(StopOperationToken stopToken)
        {
            foreach (IPlaylist playlist in Playlists.ToArray())
            {
                if (stopToken.IsStopped) return;

                await playlist.ResetSongs(stopToken);
            }
        }

        public async Task AddNew(StopOperationToken stopToken)
        {
            List<StorageFolder> folders = await GetStorageFolders();
            List<IPlaylist> adds = new List<IPlaylist>();

            foreach (StorageFolder folder in folders.OrderBy(f => f.Path))
            {
                if (stopToken.IsStopped) return;
                if (Playlists.Any(p => p.AbsolutePath == folder.Path)) continue;

                IPlaylist playlist = new Playlist(folder.Path);
                playlist.Parent = Playlists;

                await playlist.Reset(stopToken);

                if (playlist.Songs.Count > 0) adds.Add(playlist);
            }

            if (stopToken.IsStopped) return;

            Playlists.Change(null, adds);
        }

        private async Task<List<StorageFolder>> GetStorageFolders()
        {
            return await GetStorageFolders(KnownFolders.MusicLibrary);
        }

        private async Task<List<StorageFolder>> GetStorageFolders(StorageFolder folder)
        {
            List<StorageFolder> list = new List<StorageFolder>();

            try
            {
                list.Add(folder);

                IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();

                foreach (StorageFolder listFolder in folders)
                {
                    list.AddRange(await GetStorageFolders(listFolder));
                }
            }
            catch { }

            return list;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string currentPlaylistPath = reader.GetAttribute("CurrentPlaylistPath");

            try
            {
                Playlists = XmlConverter.Deserialize(new PlaylistCollection(), reader.ReadInnerXml());
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("LibraryXmlLoadFail", e, reader.Name, reader.NodeType);
                throw;
            }

            CurrentPlaylist = Playlists.FirstOrDefault(p => p.AbsolutePath == currentPlaylistPath) ?? Playlists.FirstOrDefault();

            Loaded?.Invoke(this, EventArgs.Empty);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("CurrentPlaylistPath", CurrentPlaylist?.AbsolutePath ?? "null");

            writer.WriteStartElement("Playlists");
            Playlists.WriteXml(writer);
            writer.WriteEndElement();
        }

        public ILibrary ToSimple()
        {
            ILibrary lib = new Library(IsForeground);
            lib.Playlists = Playlists.ToSimple();
            lib.CurrentPlaylist = lib.Playlists.FirstOrDefault(p => p.AbsolutePath == CurrentPlaylist?.AbsolutePath);

            //AutoSaveLoad.CheckLibrary(lib, "ToSimple");

            return lib;
        }
    }
}
