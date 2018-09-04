using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Song song = null;
            int i = 0;
            List<string> songsPaths = SkipSongs.GetSkipSongsPaths();

            if (songsPaths.Count == 0) return false;

            string lastSongPath = Current != null ? Current.Song.Path : songsPaths[0];
            ProgressType lastHandle = Current != null ? Current.Handle : ProgressType.Skip;
            IEnumerable<IPlaylist> playlists = library.Playlists;

            if (lastHandle == ProgressType.Remove)
            {
                foreach (IPlaylist playlist in playlists)
                {
                    song = playlist.Songs.FirstOrDefault(s => s.Path == lastSongPath);
                    if (song == null) continue;

                    playlist.Songs.Remove(song);
                    break;
                }
            }

            i = songsPaths.IndexOf(lastSongPath);

            if (i != -1 && Current.Handle != ProgressType.Skip) songsPaths.Remove(lastSongPath);
            else if (i == -1) i = 0;

            while (i < songsPaths.Count)
            {
                song = playlists.Select(p => p.Songs.FirstOrDefault(s => s.Path == songsPaths[i])).FirstOrDefault(s => s != null);
                if (song != null) break;

                songsPaths.RemoveAt(i);
            }

            SkipSongs.SaveSkipSongsPaths(songsPaths);

            if (song == null) return false;

            currentSkip = new SkipSong(song);
            return true;
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
