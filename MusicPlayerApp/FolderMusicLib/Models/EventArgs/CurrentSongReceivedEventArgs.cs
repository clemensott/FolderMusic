using System;

namespace MusicPlayer.Models.EventArgs
{
    public class CurrentSongReceivedEventArgs : System.EventArgs
    {
        public Song? NewSong { get; }
        
        public TimeSpan Position { get; }

        public CurrentSongReceivedEventArgs(Song? newSong, TimeSpan position)
        {
            NewSong = newSong;
            Position = position;
        }
    }
}
