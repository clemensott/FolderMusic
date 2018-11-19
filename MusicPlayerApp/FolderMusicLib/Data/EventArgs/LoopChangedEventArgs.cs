using System;

namespace MusicPlayer.Data
{
    public class LoopChangedEventArgs : EventArgs
    {
        public LoopType OldLoop { get; private set; }

        public LoopType NewLoop { get; private set; }

        internal LoopChangedEventArgs(LoopType oldLoop, LoopType newLoop)
        {
            OldLoop = oldLoop;
            NewLoop = newLoop;
        }
    }
}
