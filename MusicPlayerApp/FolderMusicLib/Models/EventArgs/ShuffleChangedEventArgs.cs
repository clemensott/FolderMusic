using MusicPlayer.Models.Shuffle;

namespace MusicPlayer.Models.EventArgs
{
    public class ShuffleChangedEventArgs : System.EventArgs
    {
        public ShuffleType OldShuffleType { get; private set; }

        public ShuffleType NewShuffleType { get; private set; }

        public IShuffleCollection OldShuffleSongs { get; private set; }

        public IShuffleCollection NewShuffleSongs { get; private set; }

        internal ShuffleChangedEventArgs(IShuffleCollection oldShuffleSongs, IShuffleCollection newShuffleSongs)
        {
            OldShuffleType = oldShuffleSongs?.Type ?? ShuffleType.Off;
            NewShuffleType = newShuffleSongs?.Type ?? ShuffleType.Off;
            OldShuffleSongs = oldShuffleSongs;
            NewShuffleSongs = newShuffleSongs;
        }
    }
}
