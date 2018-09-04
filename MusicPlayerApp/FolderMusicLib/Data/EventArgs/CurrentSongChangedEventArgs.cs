using System;

namespace MusicPlayer.Data
{
    public class CurrentSongChangedEventArgs : EventArgs
    {
        public Song OldValue { get; private set; }

        public Song NewValue { get; private set; }

        internal CurrentSongChangedEventArgs(Song oldValue, Song newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
