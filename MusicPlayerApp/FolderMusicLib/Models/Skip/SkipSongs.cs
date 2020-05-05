using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models.Skip
{
    public class SkipSongs : IEnumerable<SkipSong>
    {
        private const string skipSongsFileName = "SkipSongs.xml";

        public ILibrary Parent { get; private set; }

        public event EventHandler SkippedSong;

        internal SkipSongs(ILibrary library)
        {
            Parent = library;
        }

        public async Task<bool> HasSongs()
        {
            return (await GetSongs()).Any();
        }

        public async Task Add(Song song)
        {
            List<string> songsPaths = await GetSkipSongsPaths();
            if (songsPaths.Contains(song.Path)) return;

            songsPaths.Add(song.Path);
            await SaveSkipSongsPaths(songsPaths);

            SkippedSong?.Invoke(this, System.EventArgs.Empty);
        }

        internal static async Task<List<string>> GetSkipSongsPaths()
        {
            string text = await IO.LoadTextAsync(skipSongsFileName);
            return text.Split(';').Where(s => s.Length > 0).Distinct().ToList();
        }

        internal static async Task SaveSkipSongsPaths(IEnumerable<string> songsPaths)
        {
            string text = string.Join(";", songsPaths);

            await IO.SaveTextAsync(skipSongsFileName, text);
        }

        public IEnumerator<SkipSong> GetEnumerator()
        {
            return new SkipSongsEnumerator(Parent);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SkipSongsEnumerator(Parent);
        }

        public async Task<IEnumerable<Song>> GetSongs()
        {
            List<string> ssps = await GetSkipSongsPaths();
            IEnumerable<Song> selected = ssps.Select(ssp => Parent.Playlists.SelectMany(p => p.Songs).FirstOrDefault(s => s.Path == ssp));
            return selected.Where(s => s != null);
        }

        internal void Raise()
        {
            SkippedSong?.Invoke(this, System.EventArgs.Empty);
        }
    }
}
