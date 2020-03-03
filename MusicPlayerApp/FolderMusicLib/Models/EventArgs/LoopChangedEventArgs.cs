using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models.EventArgs
{
    public class LoopChangedEventArgs : System.EventArgs
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
