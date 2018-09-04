using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data
{
    class SkipSongsEnumerator : IEnumerator<SkipSong>
    {
        private ILibrary library;
        private SkipSong currentSkip;

        public SkipSong Current { get { return currentSkip; } }

        object IEnumerator.Current { get { return Current; } }

        public SkipSongsEnumerator(ILibrary library)
        {
            this.library = library;
        }

        public bool MoveNext()
        {
            List<string> songsPaths = SkipSongs.GetSkipSongsPaths();

            if (songsPaths.Count == 0) return false;
            MobileDebug.Manager.WriteEvent("MoveNext1", songsPaths.Count);

            int index = HandleCurrent(songsPaths);
            Song song = GetNextSong(songsPaths, index);

            SkipSongs.SaveSkipSongsPaths(songsPaths);

            MobileDebug.Manager.WriteEvent("MoveNext2", song);
            if (song == null) return false;

            currentSkip = new SkipSong(song);
            return true;
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
