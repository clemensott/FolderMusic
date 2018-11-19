﻿using MusicPlayer.Communication;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
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
        private IPlaylist currentPlaylist;
        private IPlaylistCollection playlists;
        private BackForegroundCommunicator communicator;

        public event EventHandler<PlayStateChangedEventArgs> PlayStateChanged;
        public event EventHandler<PlaylistsChangedEventArgs> PlaylistsChanged;
        public event EventHandler<CurrentPlaylistChangedEventArgs> CurrentPlaylistChanged;
        public event EventHandler SettingsChanged;
        public event EventHandler Loaded;

        public IPlaylist this[int index] { get { return Playlists.ElementAtOrDefault(index); } }

        public bool CanceledLoading { get; private set; }

        public bool IsForeground { get; private set; }

        public bool IsLoaded { get; private set; }

        public bool IsPlaying
        {
            get { return isPlaying; }
            set
            {
                if (value == isPlaying) return;

                isPlaying = value;
                var args = new PlayStateChangedEventArgs(value);
                PlayStateChanged?.Invoke(this, args);
            }
        }

        public IPlaylist CurrentPlaylist
        {
            get { return currentPlaylist; }
            set
            {
                if (value == currentPlaylist) return;

                if (!IsForeground && currentPlaylist != null)
                {
                    TimeSpan position = BackgroundMediaPlayer.Current.Position;
                    TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;
                    currentPlaylist.CurrentSongPosition = position.TotalMilliseconds / duration.TotalMilliseconds;
                }

                var args = new CurrentPlaylistChangedEventArgs(currentPlaylist, value);
                currentPlaylist = value;
                CurrentPlaylistChanged?.Invoke(this, args);
            }
        }

        public IPlaylistCollection Playlists
        {
            get { return playlists; }
            set
            {
                if (value == playlists) return;

                var args = new PlaylistsChangedEventArgs(playlists, value);
                playlists = value;
                playlists.Parent = this;
                PlaylistsChanged?.Invoke(this, args);
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

            communicator = new BackForegroundCommunicator(this);
        }

        internal Library(CurrentPlaySong currentPlaySong)
        {
            IsForeground = false;
            IsLoaded = false;

            SkippedSongs = new SkipSongs(this);
            Playlists = new PlaylistCollection(currentPlaySong);
            CurrentPlaylist = Playlists.First();

            communicator = new BackForegroundCommunicator(this);
        }

        public void Load(IEnumerable<IPlaylist> playlists)
        {
            if (playlists == null) return;

            List<IPlaylist> addPlaylists = new List<IPlaylist>();

            foreach (IPlaylist setPlaylist in playlists)
            {
                IPlaylist existingPlaylist = Playlists.FirstOrDefault(p => p.AbsolutePath == setPlaylist.AbsolutePath);

                if (existingPlaylist == null) addPlaylists.Add(setPlaylist);
                else existingPlaylist.Songs = setPlaylist.Songs;
            }

            Playlists.Change(null, addPlaylists);

            IsLoaded = true;
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        public async Task Reset()
        {
            CanceledLoading = false;
            IsPlaying = false;

            IPlaylistCollection refreshedPlaylists = new PlaylistCollection();
            var folders = await GetStorageFolders();

            foreach (StorageFolder folder in await GetStorageFolders())
            {
                IPlaylist playlist = new Playlist(folder.Path);

                await playlist.Reset();

                if (CanceledLoading) return;
                if (playlist.Songs.Count > 0) refreshedPlaylists.Add(playlist);
            }

            Playlists = refreshedPlaylists;
            currentPlaylist = playlists.FirstOrDefault();
        }

        public async Task Update()
        {
            foreach (IPlaylist playlist in Playlists.ToArray())
            {
                if (CanceledLoading) return;

                await playlist.Update();
            }
        }

        public async Task ResetSongs()
        {
            foreach (IPlaylist playlist in Playlists.ToArray())
            {
                if (CanceledLoading) return;

                await playlist.ResetSongs();
            }
        }

        public async Task AddNew()
        {
            CanceledLoading = false;

            List<IPlaylist> adds = new List<IPlaylist>();

            foreach (StorageFolder folder in (await GetStorageFolders()).OrderBy(f => f.Path))
            {
                if (CanceledLoading) return;
                if (Playlists.Any(p => p.AbsolutePath == folder.Path)) continue;

                IPlaylist playlist = new Playlist(folder.Path);
                await playlist.Reset();

                if (playlist.Songs.Count > 0) adds.Add(playlist);
            }

            if (CanceledLoading) return;

            Playlists.Change(null, adds);
        }

        private async Task AddOldOrLoadedPlaylist(string folderPath, IPlaylistCollection playlists)
        {
            IPlaylist playlist = Playlists.FirstOrDefault(x => x.AbsolutePath == folderPath);

            if (playlist == null)
            {
                playlist = new Playlist(folderPath);
                await playlist.Reset();

                if (playlist.Songs.Count == 0) return;
            }

            playlists.Add(playlist);
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

                var folders = await folder.GetFoldersAsync();

                foreach (StorageFolder listFolder in folders)
                {
                    list.AddRange(await GetStorageFolders(listFolder));
                }
            }
            catch { }

            return list;
        }

        public void CancelLoading()
        {
            CanceledLoading = true;
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

            CurrentPlaylist = playlists.FirstOrDefault(p => p.AbsolutePath == currentPlaylistPath) ?? playlists.FirstOrDefault();

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

            return lib;
        }
    }
}
