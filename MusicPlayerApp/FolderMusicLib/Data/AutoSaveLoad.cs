using MusicPlayer.Data.Shuffle;
using MusicPlayer.Data.SubscriptionsHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace MusicPlayer.Data
{
    public class AutoSaveLoad
    {
        private LibrarySubscriptionsHandler sh;

        public string Complete { get; private set; }

        public string Backup { get; private set; }

        public string Simple { get; private set; }

        public string CurrentSong { get; private set; }

        public AutoSaveLoad(string complete, string backup, string simple, string currentSong)
        {
            Complete = complete;
            Backup = backup;
            Simple = simple;
            CurrentSong = currentSong;

            sh = new LibrarySubscriptionsHandler();

            sh.PlayStateChanged += OnPlayStateChanged;
            sh.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            sh.PlaylistsPropertyChanged += OnPlaylistsPropertyChanged;
            sh.PlaylistCollectionChanged += OnPlaylistCollectionChanged;
            sh.AllPlaylists.LoopChanged += AllPlaylists_LoopChanged;
            sh.AllPlaylists.ShuffleChanged += AllPlaylists_ShuffleChanged;
            sh.AllPlaylists.ShuffleCollectionChanged += AllPlaylists_ShuffleCollectionChanged;
            sh.AllPlaylists.SongsPropertyChanged += AllPlaylists_SongsPropertyChanged;
            sh.AllPlaylists.SongCollectionChanged += AllPlaylists_SongCollectionChanged;
            sh.AllPlaylists.AllSongs.SomethingChanged += AllPlaylists_AllSongs_SomethingChanged;
            sh.CurrentPlaylist.CurrentSongChanged += CurrentPlaylist_CurrentSongChanged;
            sh.CurrentPlaylist.CurrentSongPositionChanged += CurrentPlaylist_CurrentSongPositionChanged;
            sh.CurrentPlaylist.AllSongs.SomethingChanged += CurrentPlaylist_AllSongs_SomethingChanged;
            sh.CurrentPlaylist.CurrentSong.SomethingChanged += CurrentPlaylist_CurrentSong_SomethingChanged;
            sh.OtherPlaylists.CurrentSongPositionChanged += OtherPlaylists_CurrentSongPositionChanged;
        }

        private async void OnPlayStateChanged(object sender, SubscriptionsEventArgs<ILibrary, PlayStateChangedEventArgs> e)
        {
            if(e.Base.NewValue)return;

              await SaveSimple(e.Source);
            await SaveCurrentSong(e.Source);
        }

        private async void OnCurrentPlaylistChanged(object sender, SubscriptionsEventArgs<ILibrary, CurrentPlaylistChangedEventArgs> e)
        {
            await SaveAll(e.Source);
        }

        private async void OnPlaylistsPropertyChanged(object sender, SubscriptionsEventArgs<ILibrary, PlaylistsChangedEventArgs> e)
        {
            await SaveSimple(e.Source);
            await SaveComplete(e.Source);
        }

        private async void OnPlaylistCollectionChanged(object sender, SubscriptionsEventArgs<IPlaylistCollection, PlaylistCollectionChangedEventArgs> e)
        {
            await SaveSimple(e.Source.Parent);
            await SaveComplete(e.Source.Parent);
        }

        private async void AllPlaylists_LoopChanged(object sender, SubscriptionsEventArgs<IPlaylist, LoopChangedEventArgs> e)
        {
            await SaveSimple(e.Source.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent);
        }

        private async void AllPlaylists_ShuffleChanged(object sender, SubscriptionsEventArgs<ISongCollection, ShuffleChangedEventArgs> e)
        {
            await SaveSimple(e.Source.Parent.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent.Parent);
        }

        private async void AllPlaylists_ShuffleCollectionChanged(object sender, SubscriptionsEventArgs<IShuffleCollection, ShuffleCollectionChangedEventArgs> e)
        {
            await SaveSimple(e.Source.Parent.Parent.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent.Parent.Parent);
        }

        private async void AllPlaylists_SongsPropertyChanged(object sender, SubscriptionsEventArgs<IPlaylist, SongsChangedEventArgs> e)
        {
            await SaveComplete(e.Source.Parent.Parent);
        }

        private async void AllPlaylists_SongCollectionChanged(object sender, SubscriptionsEventArgs<ISongCollection, SongCollectionChangedEventArgs> e)
        {
            await SaveComplete(e.Source.Parent.Parent.Parent);
        }

        private async void AllPlaylists_AllSongs_SomethingChanged(object sender, SubscriptionsEventArgs<Song, EventArgs> e)
        {
            await SaveSimple(e.Source.Parent.Parent.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent.Parent.Parent);
        }

        private async void CurrentPlaylist_CurrentSongChanged(object sender, SubscriptionsEventArgs<IPlaylist, CurrentSongChangedEventArgs> e)
        {
            await SaveSimple(e.Source.Parent.Parent);
            await SaveCurrentSong(e.Source.Parent.Parent);
        }

        private async void CurrentPlaylist_CurrentSongPositionChanged(object sender, SubscriptionsEventArgs<IPlaylist, CurrentSongPositionChangedEventArgs> e)
        {
            if(e.Source.Parent.Parent.IsPlaying) return;

            await SaveSimple(e.Source.Parent.Parent);
            await SaveCurrentSong(e.Source.Parent.Parent);
        }

        private async void CurrentPlaylist_AllSongs_SomethingChanged(object sender, SubscriptionsEventArgs<Song, EventArgs> e)
        {
            await SaveSimple(e.Source.Parent.Parent.Parent.Parent);
            await SaveCurrentSong(e.Source.Parent.Parent.Parent.Parent);
        }

        private async void CurrentPlaylist_CurrentSong_SomethingChanged(object sender, SubscriptionsEventArgs<Song, EventArgs> e)
        {
            await SaveSimple(e.Source.Parent.Parent.Parent.Parent);
            await SaveCurrentSong(e.Source.Parent.Parent.Parent.Parent);
        }

        private async void OtherPlaylists_CurrentSongPositionChanged(object sender, SubscriptionsEventArgs<IPlaylist, CurrentSongPositionChangedEventArgs> e)
        {
            await SaveSimple(e.Source.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent);
        }

        public void Add(ILibrary lib)
        {
            sh.Subscribe(lib);
        }

        public void Remove(ILibrary lib)
        {
            sh.Unsubscribe(lib);
        }

        private async Task SaveAll(ILibrary lib)
        {
            MobileDebug.Service.WriteEvent("SaveAll");

            await SaveComplete(lib);
            await SaveSimple(lib);
            await SaveCurrentSong(lib);
        }

        private async Task SaveComplete(ILibrary lib)
        {
            MobileDebug.Service.WriteEvent("SaveComplete", lib?.Playlists != null && lib.Playlists.Count > 0);

            try
            {
                if (lib?.Playlists != null && lib.Playlists.Count > 0) await IO.SaveObjectAsync(Complete, lib);
                else
                {
                    await IO.DeleteAsync(Complete);
                    await IO.DeleteAsync(Backup);
                }
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SaveCompleteFail", e);
            }
        }

        private async Task SaveSimple(ILibrary lib)
        {
            MobileDebug.Service.WriteEvent("SaveSimple", lib?.Playlists != null && lib.Playlists.Count > 0);

            try
            {
                if (lib?.Playlists != null && lib.Playlists.Count > 0) await IO.SaveObjectAsync(Simple, lib.ToSimple());
                else await IO.DeleteAsync(Simple);

            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SaveSimpleFail", e);
            }
        }

        private async Task SaveCurrentSong(ILibrary lib)
        {
            MobileDebug.Service.WriteEvent("SaveCurrentSong", lib?.CurrentPlaylist?.CurrentSong != null);

            try
            {
                if (lib?.CurrentPlaylist?.CurrentSong != null)
                {
                    await IO.SaveObjectAsync(CurrentSong, new CurrentPlaySong(lib));
                }
                else await IO.DeleteAsync(CurrentSong);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SaveCurrentSongFail", e);
            }
        }

        public async Task<ILibrary> LoadSimple(bool isForeground)
        {
            ILibrary library;

            try
            {
                if (isForeground)
                {
                    library = new Library(true);
                    string xmlText = await IO.LoadTextAsync(Simple);
                    MobileDebug.Service.WriteEvent("LoadSimpleForeground", xmlText);
                    library.ReadXml(XmlConverter.GetReader(xmlText));
                }
                else
                {
                    library = new Library(await IO.LoadObjectAsync<CurrentPlaySong>(CurrentSong));
                    MobileDebug.Service.WriteEvent("LoadSimpleBack", library?.CurrentPlaylist?.CurrentSong?.Path);
                }
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SimpleLibraryLoadFail", e);
                library = new Library(isForeground);
            }

            library.BeginCommunication();

            return library;
        }

        public async Task LoadComplete(ILibrary lib)
        {
            ILibrary completeLib = await GetLibrary(lib);
            lib.Load(completeLib.Playlists);
        }

        private async Task<ILibrary> GetLibrary(ILibrary lib)
        {
            ILibrary completeLib = new Library(lib.IsForeground);

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    completeLib.ReadXml(XmlConverter.GetReader(await IO.LoadTextAsync(Complete)));
                    IO.CopyAsync(Complete, Backup);
                    return completeLib;
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
                    completeLib.ReadXml(XmlConverter.GetReader(await IO.LoadTextAsync(Backup)));
                    IO.CopyAsync(Backup, Complete);
                    return completeLib;
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("Coundn't load backup", e);
                }
            }

            MobileDebug.Service.WriteEvent("Coundn't load any data");
            IO.CopyAsync(Complete, KnownFolders.VideosLibrary, Complete);
            IO.CopyAsync(Backup, KnownFolders.VideosLibrary, Backup);

            return completeLib;
        }

        public static string CheckLibrary(ILibrary lib, string id = "None")
        {
            MobileDebug.Service.WriteEvent("CheckLibraryStart", lib?.Playlists?.Count.ToString() ?? "null", id);
            bool contains = lib.Playlists.Contains(lib.CurrentPlaylist);

            List<string> list = new List<string>()
            {
                "ID: " + id,
                "CurrentPlaylist == null: " + (lib.CurrentPlaylist == null),
                "CurrentPlaylistPath: " + lib?.CurrentPlaylist?.AbsolutePath,
                "ContainsCurrentPlaylist: " + contains,
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
                    text += "\nShuffle: " + (p?.Songs?.Shuffle?.Count.ToString() ?? "null");

                    text += "\nHash: " + (p?.GetHashCode() ?? -1);
                }

                list.Add(text);
            }

            MobileDebug.Service.WriteEvent("CheckLibraryEnd", list.AsEnumerable());

            return string.Join("\r\n", list);
        }

    }
}
