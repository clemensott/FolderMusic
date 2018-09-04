using System;

namespace MusicPlayer.Data
{
    public class PlayStateChangedEventArgs:EventArgs
    {
        public bool NewValue { get; private set; }

        internal PlayStateChangedEventArgs(bool newValue)
        {
            NewValue = newValue;
        }
    }
}
