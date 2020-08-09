using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using MusicPlayer.Models;
using MusicPlayer.Models.Foreground;
using MusicPlayer.Models.Foreground.Interfaces;

namespace MusicPlayer.UpdateLibrary
{
    public static class UpdateLibraryUtils
    {
        public static Task Update(this ILibrary library, out ParentUpdateProgress progress)
        {
            return Update(library, false, out progress);
        }
        public static Task Update(this ILibrary library, bool fast, out ParentUpdateProgress progress)
        {
            progress = new ParentUpdateProgress(new CancelOperationToken());
            return UpdateLibrary(library, fast, progress);
        }

        private static async Task UpdateLibrary(ILibrary library, bool fast, ParentUpdateProgress progress)
        {
            progress.CurrentStepName = "Fetch folders";
            IList<StorageFolder> folders = await GetAllStorageFolders(KnownFolders.MusicLibrary);

            if (progress.CancelToken.IsCanceled) return;

            progress.TotalCount = folders.Count;

            Dictionary<string, IPlaylist> oldPlaylists = library.Playlists.ToDictionary(p => p.AbsolutePath);
            IList<IPlaylist> addPlaylists = new List<IPlaylist>();

            foreach (StorageFolder folder in folders)
            {
                ChildUpdateProgress childProgress = progress.Next();
                IPlaylist playlist;
                if (oldPlaylists.TryGetValue(folder.Path, out playlist))
                {
                    progress.CurrentStepName = "Update Playlist:\r\n" + playlist.Name;
                    if (fast) await UpdatePlaylistFast(playlist, folder, childProgress);
                    else await UpdatePlaylist(playlist, folder, childProgress);
                }
                else
                {
                    progress.CurrentStepName = "Create Playlist:\r\n" + folder.Name;
                    playlist = await CreatePlaylist(folder, childProgress);
                    if (childProgress.CancelToken.Result == CancelTokenResult.Completed) addPlaylists.Add(playlist);
                }
            }

            progress.CurrentStepName = "Update Playlists";
            progress.FinishChildren();

            IEnumerable<IPlaylist> removePlaylists = oldPlaylists.Values
                .Where(playlist => folders.All(f => f.Path != playlist.AbsolutePath)).ToArray();

            library.Playlists.Change(removePlaylists, addPlaylists);
            progress.CancelToken.Complete();
        }

        private static async Task<IList<StorageFolder>> GetAllStorageFolders(StorageFolder folder)
        {
            IList<StorageFolder> allFolders = new List<StorageFolder>();
            Queue<StorageFolder> queue = new Queue<StorageFolder>();
            queue.Enqueue(folder);

            while (queue.Count > 0)
            {
                folder = queue.Dequeue();
                allFolders.Add(folder);

                foreach (StorageFolder subFolder in await folder.GetFoldersAsync())
                {
                    queue.Enqueue(subFolder);
                }
            }

            return allFolders;
        }

        private static async Task<IPlaylist> CreatePlaylist(IStorageFolder folder, ChildUpdateProgress progress)
        {
            IPlaylist playlist = new Playlist(folder.Path);
            await UpdatePlaylist(playlist, folder, progress);
            return playlist;
        }

        public static Task Update(this IPlaylist playlist, out ChildUpdateProgress progress)
        {
            progress = new ChildUpdateProgress(new CancelOperationToken());
            return UpdatePlaylist(playlist, progress);
        }

        private static async Task UpdatePlaylist(IPlaylist playlist, ChildUpdateProgress progress)
        {
            await UpdatePlaylist(playlist, await GetStorageFolder(playlist.AbsolutePath), progress);
        }

        private static async Task UpdatePlaylist(IPlaylist playlist, IStorageFolder folder, ChildUpdateProgress progress)
        {
            progress.CurrentStepName = "Fetch Files";
            IReadOnlyList<StorageFile> newFiles = await folder.GetFilesAsync();
            if (progress.CancelToken.IsCanceled) return;

            progress.CurrentStepName = "Load Songs";
            Song[] newSongs = (await GetSongsFromStorageFiles(newFiles.ToArray(), progress)).ToArray();
            if (progress.CancelToken.IsCanceled) return;

            progress.CurrentStepName = "Update Songs of Playlist";
            Song[] oldSongs = playlist.Songs.ToArray();
            IEnumerable<Song> addSongs = newSongs.Except(oldSongs);
            IEnumerable<Song> removeSongs = oldSongs.Except(newSongs);
            if (progress.CancelToken.IsCanceled) return;

            playlist.Songs.Change(removeSongs, addSongs);
            progress.CancelToken.Complete();
        }

        public static Task UpdateFast(this IPlaylist playlist, out ChildUpdateProgress progress)
        {
            progress = new ChildUpdateProgress(new CancelOperationToken());
            return UpdatePlaylistFast(playlist, progress);
        }

        private static async Task UpdatePlaylistFast(IPlaylist playlist, ChildUpdateProgress progress)
        {
            await UpdatePlaylistFast(playlist, await GetStorageFolder(playlist.AbsolutePath), progress);
        }

        private static async Task UpdatePlaylistFast(IPlaylist playlist, IStorageFolder folder, ChildUpdateProgress progress)
        {
            progress.CurrentStepName = "Fetch Files";
            IReadOnlyList<StorageFile> newFiles = await folder.GetFilesAsync();
            if (progress.CancelToken.IsCanceled) return;

            progress.CurrentStepName = "Load Songs";
            IDictionary<string, Song> oldSongs = playlist.Songs.ToDictionary(s => s.FullPath);
            IEnumerable<StorageFile> addFiles = newFiles.Where(f => !oldSongs.ContainsKey(f.Path));
            IEnumerable<Song> addSongs = await GetSongsFromStorageFiles(addFiles.ToArray(), progress);
            if (progress.CancelToken.IsCanceled) return;

            IEnumerable<Song> removeSongs = oldSongs.Values
                .Where(song => newFiles.All(f => f.Path != song.FullPath));

            progress.CurrentStepName = "Update Songs of Playlist";
            playlist.Songs.Change(removeSongs, addSongs);
            progress.CancelToken.Complete();
        }

        private static async Task<StorageFolder> GetStorageFolder(string path)
        {
            if (path == string.Empty) return KnownFolders.MusicLibrary;

            return await StorageFolder.GetFolderFromPathAsync(path);
        }

        private static async Task<IEnumerable<Song>> GetSongsFromStorageFiles(IReadOnlyCollection<StorageFile> files,
            ChildUpdateProgress progress)
        {
            progress.TotalCount = files.Count;
            List<Song> songs = new List<Song>();
            foreach (StorageFile file in files)
            {
                Song? newSong = await LoadSong(file);
                if (progress.CancelToken.IsCanceled) return songs;

                progress.Increase();
                if (newSong.HasValue) songs.Add(newSong.Value);
            }

            return songs;
        }

        public static async Task<Song?> LoadSong(StorageFile file)
        {
            try
            {
                MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                return new Song()
                {
                    Title = string.IsNullOrWhiteSpace(properties.Title) ? file.Name : properties.Title,
                    Artist = properties.Artist,
                    Duration = properties.Duration,
                    FullPath = file.Path,
                };
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("Load song error", e, file.Path);
                return null;
            }
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
                    text += "\nSong: " + (p.CurrentSong.FullPath ?? "null");
                    text += "\nContainsCurrentSong: " + (p.Songs?.Contains(p.CurrentSong).ToString() ?? "null");
                    text += "\nPos: " + (p.Position.ToString() ?? "null");
                    text += "\nLoop: " + (p.Loop.ToString() ?? "null");
                    text += "\nSongs: " + (p.Songs?.Count.ToString() ?? "null");
                    text += "\nDif: " + (p.Songs?.GroupBy(s => s.FullPath ?? "null")?.Count().ToString() ?? "null");
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
