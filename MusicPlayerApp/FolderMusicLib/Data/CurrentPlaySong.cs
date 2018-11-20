using System;
using System.Threading.Tasks;

namespace MusicPlayer.Data
{
    public struct CurrentPlaySong
    {
        private const string fileName = "CurrentPlaySong.xml";

        public double Position { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Path { get; set; }

        public CurrentPlaySong(ILibrary library)
        {
            Position = library.CurrentPlaylist.CurrentSongPosition;

            Title = library.CurrentPlaylist.CurrentSong.Title;
            Artist = library.CurrentPlaylist.CurrentSong.Artist;
            Path = library.CurrentPlaylist.CurrentSong.Path;
        }

        public async static Task Delete()
        {
            await IO.DeleteAsync(fileName);
        }
    }
}
