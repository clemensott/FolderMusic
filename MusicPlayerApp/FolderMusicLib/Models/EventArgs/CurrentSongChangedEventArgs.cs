using System;

namespace MusicPlayer.Models.EventArgs
{
    public class CurrentSongChangedEventArgs : System.EventArgs
    {
        public Song? NewSong { get; }

        public CurrentSongChangedEventArgs(Song? newSong)
        {
            NewSong = newSong;
        }
    }
}
