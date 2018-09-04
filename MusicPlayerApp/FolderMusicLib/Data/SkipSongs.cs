using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace MusicPlayer.Data
{
    public class SkipSongs : IEnumerable<SkipSong>, IEnumerator<SkipSong>
    {
        private const string skipSongsFileName = "SkipSongs.xml";

        private static SkipSongs instance;

        public static SkipSongs Instance
        {
            get
            {
                if (instance == null) instance = new SkipSongs();

                return instance;
            }
        }

        private SkipSong currentSkip;

        public SkipSong Current { get { return currentSkip; } }

        object IEnumerator.Current { get { return Current; } }

        private SkipSongs()
        {
            currentSkip = new SkipSong(new Song());
        }

        public void Add(Song song)
        {
            List<string> songsPaths = GetSkipSongsPaths();
            songsPaths.Add(song.Path);

            SaveSkipSongsPaths(songsPaths);

            Feedback.Current.RaiseSkippedSongsPropertyChanged();
        }

        public List<string> GetSkipSongsPaths()
        {
            return IO.LoadText(skipSongsFileName).Split(';').Where(s => s.Length > 0).Distinct().ToList();
        }

        private void SaveSkipSongsPaths(IEnumerable<string> songsPaths)
        {
            string text = string.Join(";", songsPaths);

            IO.SaveText(skipSongsFileName, text);
        }

        public void Delete()
        {
            IO.Delete(skipSongsFileName);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        IEnumerator<SkipSong> IEnumerable<SkipSong>.GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return false;
            Song song;
            IEnumerable<Playlist> playlists = Library.Current.Playlists;

            if (Current.Handle == ProgressType.Remove) Library.Current.Playlists.FirstOrDefault(p => p.Songs.Remove(Current.Song));

            List<string> songsPaths = GetSkipSongsPaths();

            while (true)
            {
                songsPaths.Remove(Current.Song.Path);

                if (songsPaths.Count == 0) return false;

                song = playlists.Select(p => p.Songs.FirstOrDefault(s => s.Path == songsPaths[0])).Where(s => s != null).FirstOrDefault();

                if (song != null) break;

                if (songsPaths.Count == 0)
                {
                    SaveSkipSongsPaths(songsPaths);
                    currentSkip = new SkipSong(new Song());

                    return false;
                }
            }

            SaveSkipSongsPaths(songsPaths);
            currentSkip = new SkipSong(song);

            return true;
        }

        public void Reset()
        {
            currentSkip = new SkipSong(new Song());
        }

        public void Dispose()
        {
            currentSkip = new SkipSong(new Song());
        }
    }
}
