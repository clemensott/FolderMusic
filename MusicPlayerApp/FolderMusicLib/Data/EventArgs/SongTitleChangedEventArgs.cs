using System;

namespace MusicPlayer.Data
{
    public class SongTitleChangedEventArgs : EventArgs
    {
        public string OldValue { get; private set; }

        public string NewValue { get; private set; }

        internal SongTitleChangedEventArgs(string oldValue, string newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
