using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace MusicPlayer.Data
{
    public class AutoSaveLoad
    {
        public string Complete { get; private set; }

        public string Backup { get; private set; }

        public string Simple { get; private set; }

        public string CurrentSong { get; private set; }

        public ILibrary Library { get; private set; }

        public AutoSaveLoad(string complete, string backup, string simple, string currentSong)
        {
            Complete = complete;
            Backup = backup;
            Simple = simple;
            CurrentSong = currentSong;
        }

        private void Add(ILibrary lib)
        {
            if (lib == null) return;

            if (lib.IsLoaded)
            {
                lib.PlayStateChanged += OnPlayStateChanged;
                lib.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
                lib.PlaylistsChanged += OnPlaylistsPropertyChanged;

                AddCurrentPlaylist(lib.CurrentPlaylist);
                Add(lib.Playlists);
            }
            else lib.Loaded += OnLoaded;
        }

        private void Remove(ILibrary lib)
        {
            if (lib == null) return;

            lib.Loaded -= OnLoaded;
            lib.PlayStateChanged -= OnPlayStateChanged;
            lib.CurrentPlaylistChanged -= OnCurrentPlaylistChanged;
            lib.PlaylistsChanged -= OnPlaylistsPropertyChanged;

            RemoveCurrentPlaylist(lib.CurrentPlaylist);
            Remove(lib.Playlists);
        }

        private void AddCurrentPlaylist(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongPositionChanged += OnCurrentSongPositionChanged;
        }

        private void RemoveCurrentPlaylist(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongPositionChanged -= OnCurrentSongPositionChanged;
        }

        private void Add(IPlaylistCollection playlists)
        {
            if (playlists == null) return;

            playlists.Changed += OnPlaylistsCollectionChanged;

            Add((IEnumerable<IPlaylist>)playlists);
        }

        private void Remove(IPlaylistCollection playlists)
        {
            if (playlists == null) return;

            playlists.Changed -= OnPlaylistsCollectionChanged;

            Remove((IEnumerable<IPlaylist>)playlists);
        }

        private void Add(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists) Add(playlist);
        }

        private void Remove(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists) Remove(playlist);
        }

        private void Add(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.LoopChanged += OnLoopChanged;
            playlist.SongsChanged += OnSongsPropertyChanged;

            Add(playlist.Songs);
        }

        private void Remove(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.SongsChanged -= OnSongsPropertyChanged;

            Remove(playlist.Songs);
        }

        private void Add(ISongCollection songs)
        {
            if (songs == null) return;

            songs.Changed += OnSongsCollectionChanged;
            songs.ShuffleChanged += OnShufflePropertyChanged;

            Add(songs.Shuffle);
            Add((IEnumerable<Song>)songs);
        }

        private void Remove(ISongCollection songs)
        {
            if (songs == null) return;

            songs.Changed -= OnSongsCollectionChanged;
            songs.ShuffleChanged -= OnShufflePropertyChanged;

            Remove(songs.Shuffle);
            Remove((IEnumerable<Song>)songs);
        }

        private void Add(IShuffleCollection shuffle)
        {
            if (shuffle == null) return;

            shuffle.Changed += OnShuffleCollectionChanged;
        }

        private void Remove(IShuffleCollection shuffle)
        {
            if (shuffle == null) return;

            shuffle.Changed -= OnShuffleCollectionChanged;
        }

        private void Add(IEnumerable<Song> songs)
        {
            foreach (Song song in songs) Add(song);
        }

        private void Remove(IEnumerable<Song> songs)
        {
            foreach (Song song in songs) Remove(song);
        }

        private void Add(Song song)
        {
            if (song == null) return;

            song.ArtistChanged += OnSongPropertyChanged;
            song.DurationChanged += OnSongPropertyChanged;
            song.TitleChanged += OnSongPropertyChanged;
        }

        private void Remove(Song song)
        {
            if (song == null) return;

            song.ArtistChanged -= OnSongPropertyChanged;
            song.DurationChanged -= OnSongPropertyChanged;
            song.TitleChanged -= OnSongPropertyChanged;
        }

        private void OnPlayStateChanged(object sender, PlayStateChangedEventArgs e)
        {
            if (!e.NewValue) SaveSimple((ILibrary)sender);
        }

        private void OnCurrentPlaylistChanged(object sender, CurrentPlaylistChangedEventArgs e)
        {
            SaveAll((ILibrary)sender);

            RemoveCurrentPlaylist(e.OldCurrentPlaylist);
            AddCurrentPlaylist(e.NewCurrentPlaylist);
        }

        private void OnPlaylistsPropertyChanged(object sender, PlaylistsChangedEventArgs e)
        {
            SaveAll((ILibrary)sender);

            Remove(e.OldPlaylists);
            Add(e.NewPlaylists);
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            Remove((ILibrary)sender);
            Add((ILibrary)sender);
        }

        private void OnCurrentSongPositionChanged(object sender, CurrentSongPositionChangedEventArgs e)
        {
            SaveSimple(((IPlaylist)sender).Parent.Parent);
        }

        private void OnPlaylistsCollectionChanged(object sender, PlaylistCollectionChangedEventArgs e)
        {
            Remove(e.GetRemoved());
            Add(e.GetAdded());

            SaveAll(((IPlaylistCollection)sender).Parent);
        }

        private void OnCurrentSongChanged(object sender, CurrentSongChangedEventArgs e)
        {
            SaveAll(((IPlaylist)sender).Parent.Parent);
        }

        private void OnLoopChanged(object sender, LoopChangedEventArgs e)
        {
            SaveAll(((IPlaylist)sender).Parent.Parent);
        }

        private void OnSongsPropertyChanged(object sender, SongsChangedEventArgs e)
        {
            Remove(e.OldSongs);
            Add(e.NewSongs);

            SaveAll(((IPlaylist)sender).Parent.Parent);
        }

        private void OnSongsCollectionChanged(object sender, SongCollectionChangedEventArgs e)
        {
            Remove(e.GetRemoved());
            Add(e.GetAdded());

            SaveAll(((ISongCollection)sender).Parent.Parent.Parent);
        }

        private void OnShufflePropertyChanged(object sender, ShuffleChangedEventArgs e)
        {
            Remove(e.OldShuffleSongs);
            Add(e.NewShuffleSongs);

            SaveAll(((ISongCollection)sender).Parent.Parent.Parent);
        }

        private void OnShuffleCollectionChanged(object sender, ShuffleCollectionChangedEventArgs e)
        {
            SaveAll(((IShuffleCollection)sender).Parent.Parent.Parent.Parent);
        }

        private void OnSongPropertyChanged(object sender, EventArgs e)
        {

            SaveAll(((Song)sender).Parent.Parent.Parent.Parent);
        }

        private void SaveAll(ILibrary lib)
        {
            IO.SaveObject(Complete, lib);
            SaveSimple(lib);
        }

        private void SaveSimple(ILibrary lib)
        {
            IO.SaveObject(Simple, lib.ToSimple());
            IO.SaveObject(CurrentSong, new CurrentPlaySong(lib));
        }

        public ILibrary LoadSimple(bool isForeground)
        {
            ILibrary library;

            try
            {
                if (isForeground)
                {
                    library = new Library(true);
                    library.ReadXml(XmlConverter.GetReader(IO.LoadText(Simple)));
                }
                else library = new Library(IO.LoadObject<CurrentPlaySong>(CurrentSong));
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("LibraryLoadForegroundFail", e);
                library = new Library(isForeground);
            }

            return Library = library;
        }

        public void LoadComplete()
        {
            ILibrary completeLibrary = GetLibrary();
            Library.Load(completeLibrary.Playlists);
        }

        private ILibrary GetLibrary()
        {
            ILibrary lib = new Library(Library.IsForeground);

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    lib.ReadXml(XmlConverter.GetReader(IO.LoadText(Complete)));
                    IO.CopyAsync(Complete, Backup);
                    return lib;
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("Coundn't load data", e);
                }

            }

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    lib.ReadXml(XmlConverter.GetReader(IO.LoadText(Backup)));
                    IO.CopyAsync(Backup, Complete);
                    return lib;
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("Coundn't load backup", e);
                }
            }

            MobileDebug.Service.WriteEvent("Coundn't load any data");
            IO.CopyAsync(Complete, KnownFolders.VideosLibrary, Complete);
            IO.CopyAsync(Backup, KnownFolders.VideosLibrary, Backup);

            return lib;
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
                    text += "\nPos: " + (p?.CurrentSongPosition.ToString() ?? "null");
                    text += "\nLoop: " + (p?.Loop.ToString() ?? "null");
                    text += "\nSongs: " + (p?.Songs?.Count.ToString() ?? "null");
                    text += "\nDif: " + (p?.Songs?.GroupBy(s => s?.Path ?? "null")?.Count().ToString() ?? "null");
                    text += "\nShuffle: " + (p?.Songs?.Shuffle?.Type.ToString() ?? "null");
                    text += "\nShuffle: " + (p?.Songs.Shuffle?.Count.ToString() ?? "null");

                    text += "\nHash: " + (p?.GetHashCode() ?? -1);
                }

                list.Add(text);
            }

            MobileDebug.Service.WriteEvent("CheckLibraryEnd", list.AsEnumerable());

            return string.Join("\r\n", list);
        }

    }
}
