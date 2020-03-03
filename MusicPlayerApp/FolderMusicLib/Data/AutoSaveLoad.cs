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
        private readonly DoOneAtATimeHandler completeSaveHandler, simpleSaveHandler, currentSongSaveHandler;
        private LibrarySubscriptionsHandler sh;

        private string CompleteFileName { get; }

        private string BackupFileName { get; }

        private string SimpleFileName { get; }

        private string CurrentSongFileName { get; }

        public AutoSaveLoad(string completeFileName, string backupFileName, string simpleFileName, string currentSongFileName)
        {
            CompleteFileName = completeFileName;
            BackupFileName = backupFileName;
            SimpleFileName = simpleFileName;
            CurrentSongFileName = currentSongFileName;

            completeSaveHandler = new DoOneAtATimeHandler()
            {
                WaitBeforeDo = TimeSpan.FromMilliseconds(500),
                WaitAfterDo = TimeSpan.FromMilliseconds(500)
            };

            simpleSaveHandler = new DoOneAtATimeHandler()
            {
                WaitBeforeDo = TimeSpan.FromMilliseconds(500),
                WaitAfterDo = TimeSpan.FromMilliseconds(500)
            };

            currentSongSaveHandler = new DoOneAtATimeHandler()
            {
                WaitBeforeDo = TimeSpan.FromMilliseconds(500),
                WaitAfterDo = TimeSpan.FromMilliseconds(500)
            };
        }

        private async void OnPlayStateChanged(object sender, SubscriptionsEventArgs<ILibrary, IsPlayingChangedEventArgs> e)
        {
            if (e.Base.NewValue) return;
            //MobileDebug.Service.WriteEvent("ASL.OnPlayStateChanged");
            await SaveSimple(e.Source);
            await SaveCurrentSong(e.Source);
        }

        private async void OnCurrentPlaylistChanged(object sender, SubscriptionsEventArgs<ILibrary, CurrentPlaylistChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.OnCurrentPlaylistChanged");
            await SaveAll(e.Source);
        }

        private async void OnPlaylistsPropertyChanged(object sender, SubscriptionsEventArgs<ILibrary, PlaylistsChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.OnPlaylistsPropertyChanged");
            await SaveSimple(e.Source);
            await SaveComplete(e.Source);
        }

        private async void OnPlaylistCollectionChanged(object sender, SubscriptionsEventArgs<IPlaylistCollection, PlaylistCollectionChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.OnPlaylistCollectionChanged");
            await SaveSimple(e.Source.Parent);
            await SaveComplete(e.Source.Parent);
        }

        private async void AllPlaylists_LoopChanged(object sender, SubscriptionsEventArgs<IPlaylist, LoopChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.AllPlaylists_LoopChanged");
            await SaveSimple(e.Source.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent);
        }

        private async void AllPlaylists_ShuffleChanged(object sender, SubscriptionsEventArgs<ISongCollection, ShuffleChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.AllPlaylists_ShuffleChanged");
            await SaveSimple(e.Source.Parent.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent.Parent);
        }

        private async void AllPlaylists_ShuffleCollectionChanged(object sender, SubscriptionsEventArgs<IShuffleCollection, ShuffleCollectionChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.AllPlaylists_ShuffleCollectionChanged");
            await SaveSimple(e.Source.Parent.Parent.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent.Parent.Parent);
        }

        private async void AllPlaylists_SongsPropertyChanged(object sender, SubscriptionsEventArgs<IPlaylist, SongsChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.AllPlaylists_SongsPropertyChanged");
            await SaveComplete(e.Source.Parent.Parent);
        }

        private async void AllPlaylists_SongCollectionChanged(object sender, SubscriptionsEventArgs<ISongCollection, SongCollectionChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.AllPlaylists_SongCollectionChanged");
            await SaveComplete(e.Source.Parent.Parent.Parent);
        }

        private async void AllPlaylists_AllSongs_SomethingChanged(object sender, SubscriptionsEventArgs<Song, EventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.AllPlaylists_AllSongs_SomethingChanged");
            await SaveSimple(e.Source.Parent.Parent.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent.Parent.Parent);
        }

        private async void CurrentPlaylist_CurrentSongChanged(object sender, SubscriptionsEventArgs<IPlaylist, CurrentSongChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.CurrentPlaylist_CurrentSongChanged");
            await SaveSimple(e.Source.Parent.Parent);
            await SaveCurrentSong(e.Source.Parent.Parent);
        }

        private async void CurrentPlaylist_CurrentSongPositionChanged(object sender, SubscriptionsEventArgs<IPlaylist, CurrentSongPositionChangedEventArgs> e)
        {
            if (e.Source.Parent.Parent.IsPlaying) return;
            //MobileDebug.Service.WriteEvent("ASL.CurrentPlaylist_CurrentSongPositionChanged");
            await SaveSimple(e.Source.Parent.Parent);
            await SaveCurrentSong(e.Source.Parent.Parent);
        }

        private async void CurrentPlaylist_AllSongs_SomethingChanged(object sender, SubscriptionsEventArgs<Song, EventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.CurrentPlaylist_AllSongs_SomethingChanged");
            await SaveSimple(e.Source.Parent.Parent.Parent.Parent);
            await SaveCurrentSong(e.Source.Parent.Parent.Parent.Parent);
        }

        private async void CurrentPlaylist_CurrentSong_SomethingChanged(object sender, SubscriptionsEventArgs<Song, EventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.CurrentPlaylist_CurrentSong_SomethingChanged");
            await SaveSimple(e.Source.Parent.Parent.Parent.Parent);
            await SaveCurrentSong(e.Source.Parent.Parent.Parent.Parent);
        }

        private async void OtherPlaylists_CurrentSongPositionChanged(object sender, SubscriptionsEventArgs<IPlaylist, CurrentSongPositionChangedEventArgs> e)
        {
            //MobileDebug.Service.WriteEvent("ASL.OtherPlaylists_CurrentSongPositionChanged");
            await SaveSimple(e.Source.Parent.Parent);
            await SaveComplete(e.Source.Parent.Parent);
        }

        public void Add(ILibrary lib)
        {
            sh = LibrarySubscriptionsHandler.GetInstance(lib);

            sh.IsPlayingChanged += OnPlayStateChanged;
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

        public void Remove(ILibrary lib)
        {
            sh.Unsubscribe(lib);

            sh.IsPlayingChanged -= OnPlayStateChanged;
            sh.CurrentPlaylistChanged -= OnCurrentPlaylistChanged;
            sh.PlaylistsPropertyChanged -= OnPlaylistsPropertyChanged;
            sh.PlaylistCollectionChanged -= OnPlaylistCollectionChanged;
            sh.AllPlaylists.LoopChanged -= AllPlaylists_LoopChanged;
            sh.AllPlaylists.ShuffleChanged -= AllPlaylists_ShuffleChanged;
            sh.AllPlaylists.ShuffleCollectionChanged -= AllPlaylists_ShuffleCollectionChanged;
            sh.AllPlaylists.SongsPropertyChanged -= AllPlaylists_SongsPropertyChanged;
            sh.AllPlaylists.SongCollectionChanged -= AllPlaylists_SongCollectionChanged;
            sh.AllPlaylists.AllSongs.SomethingChanged -= AllPlaylists_AllSongs_SomethingChanged;
            sh.CurrentPlaylist.CurrentSongChanged -= CurrentPlaylist_CurrentSongChanged;
            sh.CurrentPlaylist.CurrentSongPositionChanged -= CurrentPlaylist_CurrentSongPositionChanged;
            sh.CurrentPlaylist.AllSongs.SomethingChanged -= CurrentPlaylist_AllSongs_SomethingChanged;
            sh.CurrentPlaylist.CurrentSong.SomethingChanged -= CurrentPlaylist_CurrentSong_SomethingChanged;
            sh.OtherPlaylists.CurrentSongPositionChanged -= OtherPlaylists_CurrentSongPositionChanged;
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
            try
            {
                await completeSaveHandler.DoAsync(async () =>
                {
                    if (lib?.Playlists != null && lib.Playlists.Count > 0) await IO.SaveObjectAsync(CompleteFileName, lib);
                    else
                    {
                        await IO.DeleteAsync(CompleteFileName);
                        await IO.DeleteAsync(BackupFileName);
                    }
                });
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SaveCompleteFail", e);
            }
        }

        private async Task SaveSimple(ILibrary lib)
        {
            try
            {
                await simpleSaveHandler.DoAsync(async () =>
                {
                    if (lib?.Playlists != null && lib.Playlists.Count > 0) await IO.SaveObjectAsync(SimpleFileName, lib.ToSimple());
                    else await IO.DeleteAsync(SimpleFileName);
                });
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SaveSimpleFail", e);
            }

        }

        private async Task SaveCurrentSong(ILibrary lib)
        {
            try
            {
                await currentSongSaveHandler.DoAsync(async () =>
                {
                    if (lib?.CurrentPlaylist?.CurrentSong != null)
                    {
                        await IO.SaveObjectAsync(CurrentSongFileName, new CurrentPlaySong(lib));
                    }
                    else await IO.DeleteAsync(CurrentSongFileName);
                });
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
                    string xmlText = await IO.LoadTextAsync(SimpleFileName);
                    //MobileDebug.Service.WriteEvent("LoadSimpleForeground", xmlText);
                    library.ReadXml(XmlConverter.GetReader(xmlText));
                }
                else
                {
                    library = new Library(await IO.LoadObjectAsync<CurrentPlaySong>(CurrentSongFileName));
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
                    completeLib.ReadXml(XmlConverter.GetReader(await IO.LoadTextAsync(CompleteFileName)));
                    IO.CopyAsync(CompleteFileName, BackupFileName);
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
                    completeLib.ReadXml(XmlConverter.GetReader(await IO.LoadTextAsync(BackupFileName)));
                    IO.CopyAsync(BackupFileName, CompleteFileName);
                    return completeLib;
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("Coundn't load backupFileName", e);
                }
            }

            MobileDebug.Service.WriteEvent("Coundn't load any data");
            IO.CopyAsync(CompleteFileName, KnownFolders.VideosLibrary, CompleteFileName);
            IO.CopyAsync(BackupFileName, KnownFolders.VideosLibrary, BackupFileName);

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
                    text += "\nName: " + (p.Name ?? "null");
                    text += "\nPath: " + (p.AbsolutePath ?? "null");
                    text += "\nSong: " + (p.CurrentSong?.Path ?? "null");
                    text += "\nContainsCurrentSong: " + (p.Songs?.Contains(p.CurrentSong).ToString() ?? "null");
                    text += "\nPos: " + (p.CurrentSongPosition.ToString() ?? "null");
                    text += "\nLoop: " + (p.Loop.ToString() ?? "null");
                    text += "\nSongs: " + (p.Songs?.Count.ToString() ?? "null");
                    text += "\nDif: " + (p.Songs?.GroupBy(s => s?.Path ?? "null")?.Count().ToString() ?? "null");
                    text += "\nShuffle: " + (p.Songs?.Shuffle?.Type.ToString() ?? "null");
                    text += "\nShuffle: " + (p.Songs?.Shuffle?.GetType().Name ?? "null");
                    text += "\nShuffle: " + (p.Songs?.Shuffle?.Count.ToString() ?? "null");

                    text += "\nHash: " + p.GetHashCode();
                }

                list.Add(text);
            }

            MobileDebug.Service.WriteEvent("CheckLibraryEnd", list.AsEnumerable());

            return string.Join("\r\n", list);
        }

    }
}
