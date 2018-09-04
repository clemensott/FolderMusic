using System;

namespace MusicPlayer.Data
{
    public class CurrentSongChangedEventArgs : EventArgs
    {
        public Song OldCurrentSong { get; private set; }

        public Song NewCurrentSong { get; private set; }

        internal CurrentSongChangedEventArgs(Song oldCurrentSong, Song newCurrentSong)
        {
            OldCurrentSong = oldCurrentSong;
            NewCurrentSong = newCurrentSong;
        }
    }
}
