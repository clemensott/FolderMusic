using System;

namespace MusicPlayer.Data
{
    public class IsPlayingChangedEventArgs : EventArgs
    {
        public bool NewValue { get; private set; }

        internal IsPlayingChangedEventArgs(bool newValue)
        {
            NewValue = newValue;
        }
    }
}
