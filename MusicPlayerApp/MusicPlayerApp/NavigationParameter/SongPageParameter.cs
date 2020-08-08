using MusicPlayer.Models;
using MusicPlayer.Models.Foreground.Interfaces;

namespace FolderMusic.NavigationParameter
{
    public class SongPageParameter
    {
        public Song Song { get; }
        
        public ISongCollection Songs { get; }

        public SongPageParameter(Song song, ISongCollection songs)
        {
            Song = song;
            Songs = songs;
        }
    }
}
