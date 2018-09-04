using System;

namespace MusicPlayer.Data
{
    public class SongNaturalDurationChangedEventArgs:EventArgs
    {
        public double OldValue { get; private set; }

        public double NewValue { get; private set; }

        internal SongNaturalDurationChangedEventArgs(double oldValue, double newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
