using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace MusicPlayer.Data
{
    public delegate void SkippedSongEventHandler(SkipSongs sender);

    public class SkipSongs : IEnumerable<SkipSong>
    {
        private const string skipSongsFileName = "SkipSongs.xml";

        private ILibrary library;

        public event SkippedSongEventHandler SkippedSong;

        internal SkipSongs(ILibrary library)
        {
            this.library = library;
        }

        public void Add(Song song)
        {
            List<string> songsPaths = GetSkipSongsPaths();
            if (songsPaths.Contains(song.Path)) return;

            songsPaths.Add(song.Path);
            SaveSkipSongsPaths(songsPaths);

            SkippedSong?.Invoke(this);
        }

        internal static List<string> GetSkipSongsPaths()
        {
            return IO.LoadText(skipSongsFileName).Split(';').Where(s => s.Length > 0).Distinct().ToList();
        }

        internal static void SaveSkipSongsPaths(IEnumerable<string> songsPaths)
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
            return new SkipSongsEnumerator(library);
        }

        IEnumerator<SkipSong> IEnumerable<SkipSong>.GetEnumerator()
        {
            return new SkipSongsEnumerator(library);
        }

        public IEnumerable<Song> GetSongs()
        {
            List<string> ssps = GetSkipSongsPaths();
            var selected = ssps.Select(ssp => library.Playlists.SelectMany(p => p.Songs).FirstOrDefault(s => s.Path == ssp));
            return selected.Where(s => s != null);
        }
    }
}
