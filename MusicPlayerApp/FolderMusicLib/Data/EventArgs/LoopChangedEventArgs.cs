using MusicPlayer.Data.Loop;
using System;

namespace MusicPlayer.Data
{
    public class LoopChangedEventArgs : EventArgs
    {
        public LoopType OldValue { get; private set; }

        public LoopType NewValue { get; private set; }

        internal LoopChangedEventArgs(LoopType oldValue, LoopType newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
