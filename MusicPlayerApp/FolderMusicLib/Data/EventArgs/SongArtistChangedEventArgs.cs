using System;

namespace MusicPlayer.Data
{
    public class SongArtistChangedEventArgs : EventArgs
    {
        public string OldValue { get; private set; }

        public string NewValue { get; private set; }

        internal SongArtistChangedEventArgs(string oldValue, string newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
