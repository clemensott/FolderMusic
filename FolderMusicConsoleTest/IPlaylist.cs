using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MusicPlayer.Data
{
    internal interface IPlaylist
    {
        List<Song> Songs { get; }
    }

    class Playlist : IPlaylist
    {
        public List<Song> Songs { get; private set; }

        public Playlist()
        {
            Songs = GetRandomSongs(100).ToList();
        }

        private IEnumerable<Song> GetRandomSongs(int count)
        {
            for (int i = 0; i < count; i++) yield return new Song() { Path = Path.GetRandomFileName() };
        }
    }
}