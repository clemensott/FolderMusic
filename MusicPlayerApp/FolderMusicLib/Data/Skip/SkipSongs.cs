using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data
{
    public delegate void SkippedSongEventHandler(SkipSongs sender);

    public class SkipSongs : IEnumerable<SkipSong>
    {
        private const string skipSongsFileName = "SkipSongs.xml";

        public ILibrary Parent { get; private set; }

        public event SkippedSongEventHandler SkippedSong;

        internal SkipSongs(ILibrary library)
        {
            Parent = library;
        }

        public bool HasSongs()
        {
            return GetSongs().Count() > 0;
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

        public IEnumerator<SkipSong> GetEnumerator()
        {
            return new SkipSongsEnumerator(Parent);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SkipSongsEnumerator(Parent);
        }

        public IEnumerable<Song> GetSongs()
        {
            List<string> ssps = GetSkipSongsPaths();
            var selected = ssps.Select(ssp => Parent.Playlists.SelectMany(p => p.Songs).FirstOrDefault(s => s.Path == ssp));
            return selected.Where(s => s != null);
        }

        internal void Raise()
        {
            SkippedSong?.Invoke(this);
        }
    }
}
