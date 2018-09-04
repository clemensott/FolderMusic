using System;

namespace MusicPlayer.Data
{
    public class SongDurationChangedEventArgs:EventArgs
    {
        public double OldDuration { get; private set; }

        public double NewDuration { get; private set; }

        internal SongDurationChangedEventArgs(double oldDuration, double newDuration)
        {
            OldDuration = oldDuration;
            NewDuration = newDuration;
        }
    }
}
