using MusicPlayer.Data.Shuffle;
using System;

namespace MusicPlayer.Data
{
    public class ShuffleChangedEventArgs : EventArgs
    {
        public ShuffleType OldShuffleType { get; private set; }

        public ShuffleType NewShuffleType { get; private set; }

        public IShuffleCollection OldShuffleSongs { get; private set; }

        public IShuffleCollection NewShuffleSongs { get; private set; }

        public Song OldCurrentSong { get; private set; }

        public Song NewCurrentSong { get; private set; }

        internal ShuffleChangedEventArgs(IShuffleCollection oldShuffleSongs, IShuffleCollection newShuffleSongs,
            Song oldCurrentSong, Song newCurrentSong)
        {
            OldShuffleType = oldShuffleSongs?.Type ?? ShuffleType.Off;
            NewShuffleType = newShuffleSongs?.Type ?? ShuffleType.Off;
            OldShuffleSongs = oldShuffleSongs;
            NewShuffleSongs = newShuffleSongs;
            OldCurrentSong = oldCurrentSong;
            NewCurrentSong = newCurrentSong;
        }
    }
}
