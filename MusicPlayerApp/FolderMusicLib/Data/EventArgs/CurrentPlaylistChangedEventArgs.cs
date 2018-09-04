using System;

namespace MusicPlayer.Data
{
    public class CurrentPlaylistChangedEventArgs : EventArgs
    {
        public Playlist OldValue { get; private set; }

        public Playlist NewValue { get; private set; }

        internal CurrentPlaylistChangedEventArgs(Playlist oldValue, Playlist newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
