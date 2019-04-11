using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicPlayer.Data
{
    class SkipSongsEnumerator : IEnumerator<SkipSong>
    {
        private readonly ILibrary library;
        private SkipSong currentSkip;

        public SkipSong Current => currentSkip;

        object IEnumerator.Current => Current;

        public SkipSongsEnumerator(ILibrary library)
        {
            this.library = library;
        }

        public bool MoveNext()
        {
            List<string> songsPaths = Await(SkipSongs.GetSkipSongsPaths);

            if (songsPaths.Count == 0) return false;
            MobileDebug.Service.WriteEvent("MoveNext1", songsPaths.Count);

            int index = HandleCurrent(songsPaths);
            Song song = GetNextSong(songsPaths, index);

            Await(SkipSongs.SaveSkipSongsPaths, songsPaths);

            MobileDebug.Service.WriteEvent("MoveNext2", song);
            if (song == null) return false;

            currentSkip = new SkipSong(song);
            return true;
        }

        private static TResult Await<TResult>(Func<Task<TResult>> func)
        {
            Task<Task<TResult>> task = Task.Factory.StartNew(async () => await func());

            task.Wait();
            task.Result.Wait();

            return task.Result.Result;
        }

        private static void Await<T1>(Func<T1, Task> func, T1 param1)
        {
            Task<Task> task = Task.Factory.StartNew(async () => await func(param1));

            task.Wait();
            task.Result.Wait();
        }

        private int HandleCurrent(List<string> songsPaths)
        {
            if (Current == null) return 0;

            Song song;
            IEnumerable<IPlaylist> playlists = library.Playlists;

            int index = songsPaths.IndexOf(Current.Song.Path);

            switch (Current.Handle)
            {
                case HandleType.Remove:
                    foreach (IPlaylist playlist in playlists)
                    {
                        song = playlist.Songs.FirstOrDefault(s => s.Path == Current.Song.Path);
                        if (song == null) continue;

                        playlist.Songs.Remove(song);
                        break;
                    }

                    songsPaths.Remove(Current.Song.Path);
                    break;

                case HandleType.Keep:
                    songsPaths.Remove(Current.Song.Path);
                    break;

                case HandleType.Skip:
                    index++;
                    break;
            }

            return index;
        }

        private Song GetNextSong(List<string> songsPaths, int index)
        {
            while (index < songsPaths.Count)
            {
                Song song = library.Playlists.SelectMany(p => p.Songs).FirstOrDefault(s => s.Path == songsPaths[index]);
                if (song != null) return song;

                songsPaths.RemoveAt(index);
            }

            return null;
        }

        public void Reset()
        {
            currentSkip = null;
        }

        public void Dispose()
        {
            //currentSkip = new SkipSong(new Song());
        }
    }
}
