using MusicPlayer.Communication;
using MusicPlayer.Data.NonLoaded;
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
        public const double DefaultSongsPositionPercent = 0, DefaultSongsPositionMillis = 1;

        private const string fileName = "Data.xml", backupFileName = "Data.bak";

        private bool isPlaying;
        private bool? isForeground;
        private IPlaylist currentPlaylist;
        private IPlaylistCollection playlists;

        public event PlayStateChangedEventHandler PlayStateChanged;
        public event PlaylistsPropertyChangedEventHandler PlaylistsChanged;
        public event CurrentPlaylistPropertyChangedEventHandler CurrentPlaylistChanged;
        public event SettingsPropertyChangedEventHandler SettingsChanged;
        public event LibraryChangedEventHandler LibraryChanged;

        public IPlaylist this[int index] { get { return Playlists.ElementAtOrDefault(index); } }

        public bool CanceledLoading { get; private set; }

        public bool IsLoadedComplete { get; private set; }

        public bool IsPlaying
        {
            get { return isPlaying; }
            set
            {
                if (value == isPlaying) return;

                isPlaying = value;
                var args = new PlayStateChangedEventArgs(value);
                PlayStateChanged?.Invoke(this, args);

                if (!isPlaying) SaveSimple();
            }
        }

        public IPlaylist CurrentPlaylist
        {
            get { return currentPlaylist; }
            set
            {
                if (value == currentPlaylist || !Playlists.Contains(value)) return;

                if (isForeground == false && currentPlaylist != null)
                {
                    TimeSpan position = BackgroundMediaPlayer.Current.Position;
                    TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;
                    currentPlaylist.CurrentSongPositionPercent = position.TotalMilliseconds / duration.TotalMilliseconds;
                }

                var args = new CurrentPlaylistChangedEventArgs(currentPlaylist, value);
                currentPlaylist = value;
                CurrentPlaylistChanged?.Invoke(this, args);

                Save();
            }
        }

        public IPlaylistCollection Playlists { get { return playlists; } }

        public SkipSongs SkippedSongs { get; private set; }

        internal Library(bool isForeground)
        {
            this.isForeground = isForeground;
            SkippedSongs = new SkipSongs(this);
            playlists = new PlaylistCollection(this);
            currentPlaylist = null;
        }

        internal Library(CurrentPlaySong currentPlaySong)
        {
            isForeground = false;
            IsLoadedComplete = false;

            SkippedSongs = new SkipSongs(this);
            playlists = new NonLoadedPlaylistCollection(this, currentPlaySong);
            currentPlaylist = playlists.First();

            LibraryChanged += OnLibraryChanged;
        }

        public Library(string xmlText)
        {
            IsLoadedComplete = true;
            System.Diagnostics.Debug.WriteLine(xmlText.Length);
            ReadXml(XmlConverter.GetReader(xmlText));
        }

        public void Set(ILibrary library)
        {
            if (library == null) return;

            var args = new LibraryChangedEventsArgs(playlists, library.Playlists, currentPlaylist, library.CurrentPlaylist);

            if (isForeground == false)
            {
                ISongCollection songs = library?.CurrentPlaylist?.Songs;
                Song currentSong = songs?.FirstOrDefault(s => s.Path == CurrentPlaylist?.CurrentSong?.Path) ??
                    library.CurrentPlaylist?.Songs.FirstOrDefault();

                double currentSongPositionPercent = CurrentPlaylist?.CurrentSongPositionPercent ?? 0;

                if (currentSong != null)
                {
                    library.CurrentPlaylist.CurrentSong = currentSong;
                    library.CurrentPlaylist.CurrentSongPositionPercent = currentSongPositionPercent;
                }
            }
            else if (isForeground == true)
            {
                if (IsPlaying)
                {
                    PlayStateChanged?.Invoke(this, new PlayStateChangedEventArgs(IsPlaying));
                }
                else IsPlaying = BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing;
            }

            playlists = library.Playlists;
            currentPlaylist = library.CurrentPlaylist;
            IsLoadedComplete = true;

            LibraryChanged?.Invoke(this, args);
        }

        public async Task Refresh()
        {
            CanceledLoading = false;
            IsPlaying = false;

            IPlaylistCollection refreshedPlaylists = new PlaylistCollection(this);
            var folders = await GetStorageFolders();

            foreach (StorageFolder folder in await GetStorageFolders())
            {
                IPlaylist playlist = new Playlist(refreshedPlaylists, folder.Path);

                await playlist.Refresh();

                if (CanceledLoading) return;
                if (playlist.SongsCount > 0) refreshedPlaylists.Add(playlist);
            }

            var args = new PlaylistsChangedEventArgs(Playlists, refreshedPlaylists,
                CurrentPlaylist, refreshedPlaylists.FirstOrDefault());

            playlists = refreshedPlaylists;
            currentPlaylist = playlists.FirstOrDefault();

            PlaylistsChanged?.Invoke(this, args);
        }

        public async Task Update()
        {
            foreach (IPlaylist playlist in Playlists.ToArray())
            {
                if (CanceledLoading) return;

                await playlist.Update();
            }
        }

        public async Task AddNew()
        {
            CanceledLoading = false;

            IPlaylistCollection updatedPlaylists = new PlaylistCollection(this);

            foreach (StorageFolder folder in await GetStorageFolders())
            {
                await AddOldOrLoadedPlaylist(folder.Path, updatedPlaylists);

                if (CanceledLoading) return;
            }

            var args = new PlaylistsChangedEventArgs(Playlists, updatedPlaylists, CurrentPlaylist, CurrentPlaylist);
            playlists = updatedPlaylists;
            PlaylistsChanged?.Invoke(this, args);
        }

        private async Task AddOldOrLoadedPlaylist(string folderPath, IPlaylistCollection playlists)
        {
            IPlaylist playlist = Playlists.FirstOrDefault(x => x.AbsolutePath == folderPath);

            if (playlist == null)
            {
                playlist = new Playlist(playlists, folderPath);
                await playlist.Refresh();

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

        public async Task SaveAsync()
        {
            if (isForeground == false) await new Task(new Action(Save));
        }

        public void Save()
        {
            MobileDebug.Service.WriteEventPair("SaveLibrary", "IsForeground: ", isForeground);
            SaveSimple();

            if (isForeground != false) return;

            try
            {
                IO.SaveObject(fileName, this);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SaveFail", e);
                CheckLibrary(this);
            }
        }

        private void SaveSimple()
        {
            if (isForeground != false) return;

            try
            {
                CurrentPlaySong.Save(this);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SaveSimpleSongFail", e);
            }

            try
            {
                var non = new NonLoadedLibrary(this);
                non.Save();
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SaveSimpleLibrayFail", e);
            }
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
                playlists = new PlaylistCollection(this, reader.ReadInnerXml());
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("LibraryXmlLoadFail", e, reader.Name, reader.NodeType);
                throw;
            }

            currentPlaylist = playlists.FirstOrDefault(p => p.AbsolutePath == currentPlaylistPath) ?? playlists.FirstOrDefault();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("CurrentPlaylistPath", CurrentPlaylist?.AbsolutePath ?? "null");

            writer.WriteStartElement("Playlists");
            Playlists.WriteXml(writer);
            writer.WriteEndElement();
        }

        public static string CheckLibrary(ILibrary lib)
        {
            MobileDebug.Service.WriteEvent("CheckLibraryStart", lib?.Playlists?.Count.ToString() ?? "null");
            bool contains = lib.Playlists.Contains(lib.CurrentPlaylist);

            List<string> list = new List<string>()
            {
                "ContainsCurrentPlaylist: " + contains,
                "CurrentPlaylist==null: " + (lib.CurrentPlaylist == null),
                "LibraryType: " + lib.GetType().Name,
                "LibraryHash: " + lib.GetHashCode()
            };

            foreach (IPlaylist p in lib.Playlists)
            {
                string text = "";

                if (p != null)
                {
                    text += "\nName: " + (p?.Name ?? "null");
                    text += "\nPath: " + (p?.AbsolutePath ?? "null");
                    text += "\nSong: " + (p?.CurrentSong?.Path ?? "null");
                    text += "\nContainsCurrentSong: " + (p?.Songs?.Contains(p?.CurrentSong).ToString() ?? "null");
                    text += "\nPos: " + (p?.CurrentSongPositionPercent.ToString() ?? "null");
                    text += "\nShuffle: " + (p?.Shuffle.ToString() ?? "null");
                    text += "\nLoop: " + (p?.Loop.ToString() ?? "null");
                    text += "\nSongs: " + (p?.Songs?.Count.ToString() ?? "null");
                    text += "\nDif: " + (p?.Songs?.GroupBy(s => s?.Path ?? "null")?.Count().ToString() ?? "null");
                    text += "\nShuffle: " + (p?.ShuffleSongs?.Count.ToString() ?? "null");

                    text += "\nHash: " + (p?.GetHashCode() ?? -1);
                }

                list.Add(text);
            }

            MobileDebug.Service.WriteEvent("CheckLibraryEnd", list.AsEnumerable());

            return string.Join("\r\n", list);
        }

        public static ILibrary LoadSimple(bool isForeground)
        {
            ILibrary library;

            if (isForeground)
            {
                library = new Library(true);

                try
                {
                    ILibrary nonLoadedLibrary = NonLoadedLibrary.Load();
                    library.Set(nonLoadedLibrary);
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("LibraryLoadForegroundFail", e);
                }

                BackForegroundCommunicator.StartCommunication(library, isForeground);
            }
            else
            {
                library = CurrentPlaySong.Load();
                BackForegroundCommunicator.StartCommunication(library, isForeground);
            }

            return library;
        }

        private static ILibrary GetLibrary()
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    ILibrary lib = new Library(IO.LoadText(fileName));
                    IO.Copy(fileName, backupFileName);
                    return lib;
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("Coundn't load Data", e);
                }
            }

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    ILibrary lib = new Library(IO.LoadText(backupFileName));
                    IO.Copy(backupFileName, fileName);
                    return lib;
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("Coundn't load Backup", e);
                }
            }

            MobileDebug.Service.WriteEvent("Coundn't load any data");
            IO.Copy(fileName, KnownFolders.VideosLibrary, fileName);
            IO.Copy(backupFileName, KnownFolders.VideosLibrary, backupFileName);

            return new Library(false);
        }

        public void LoadComplete()
        {
            ILibrary completeLibrary = GetLibrary();
            Set(completeLibrary);
        }

        private void OnLibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            Unsubscribe(args.OldPlaylists);
            Subscribe(args.NewPlaylists);

            if (args.OldPlaylists != null) args.OldPlaylists.Changed -= OnPlaylistsChanged;
            if (args.NewPlaylists != null) args.NewPlaylists.Changed += OnPlaylistsChanged;
        }

        private void Subscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                Subscribe(playlist);
            }
        }

        private void Unsubscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                Unsubscribe(playlist);
            }
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.CurrentSongPositionChanged += OnCurrentSongPositionChanged;
            playlist.LoopChanged += OnLoopChanged;
            playlist.ShuffleChanged += OnShuffleChanged;

            playlist.Songs.Changed += OnSongsChanged;
            playlist.ShuffleSongs.Changed += OnShuffleSongsChanged;

            Subscribe(playlist.Songs);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.CurrentSongPositionChanged -= OnCurrentSongPositionChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.ShuffleChanged -= OnShuffleChanged;

            playlist.Songs.Changed -= OnSongsChanged;
            playlist.ShuffleSongs.Changed -= OnShuffleSongsChanged;

            Unsubscribe(playlist.Songs);
        }

        private void Subscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>()) Subscribe(song);
        }

        private void Unsubscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>()) Unsubscribe(song);
        }

        private void Subscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged += OnSongChanged;
            song.DurationChanged += OnSongChanged;
            song.TitleChanged += OnSongChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged -= OnSongChanged;
            song.DurationChanged -= OnSongChanged;
            song.TitleChanged -= OnSongChanged;
        }

        private void OnPlaylistsChanged(IPlaylistCollection sender, PlaylistCollectionChangedEventArgs args)
        {
            Unsubscribe(args.GetRemoved());
            Subscribe(args.GetAdded());

            Save();
        }

        private void OnCurrentSongChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            SaveSimple();
        }

        private void OnCurrentSongPositionChanged(IPlaylist sender, CurrentSongPositionChangedEventArgs args)
        {
            SaveSimple();
        }

        private void OnLoopChanged(IPlaylist sender, LoopChangedEventArgs args)
        {
            Save();
        }

        private void OnShuffleChanged(IPlaylist sender, ShuffleChangedEventArgs args)
        {
            args.OldShuffleSongs.Changed -= OnShuffleSongsChanged;
            args.NewShuffleSongs.Changed += OnShuffleSongsChanged;

            Save();
        }

        private void OnSongsChanged(ISongCollection sender, SongCollectionChangedEventArgs args)
        {
            Unsubscribe(args.GetRemoved());
            Subscribe(args.GetAdded());

            Save();
        }

        private void OnShuffleSongsChanged(IShuffleCollection sender)
        {
            Save();
        }

        private void OnSongChanged(Song sender, EventArgs args)
        {
            Save();
        }
    }
}
