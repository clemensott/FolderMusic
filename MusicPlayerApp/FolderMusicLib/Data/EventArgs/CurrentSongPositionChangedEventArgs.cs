using System;

namespace MusicPlayer.Data
{
    public class CurrentSongPositionChangedEventArgs : EventArgs
    {
        public double OldValue { get; private set; }

        public double NewValue { get; private set; }

        internal CurrentSongPositionChangedEventArgs(double oldValue, double newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
