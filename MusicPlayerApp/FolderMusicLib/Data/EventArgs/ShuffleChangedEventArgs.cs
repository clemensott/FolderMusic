using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;

namespace MusicPlayer.Data
{
    public class ShuffleChangedEventArgs : EventArgs
    {
        public ShuffleType OldShuffleType { get; private set; }

        public ShuffleType NewShuffleType { get; private set; }

        public List<int> OldShuffleList { get; private set; }

        public List<int> NewShuffleList { get; private set; }

        public Song OldCurrentSong { get; private set; }

        public Song NewCurrentSong { get; private set; }

        internal ShuffleChangedEventArgs(ShuffleType oldShuffleType, ShuffleType newShuffleType,
            List<int> oldShuffleList, List<int> newShuffleList, Song oldCurrentSong, Song newCurrentSong)
        {
            OldShuffleType = oldShuffleType;
            NewShuffleType = newShuffleType;
            OldShuffleList = oldShuffleList;
            NewShuffleList = newShuffleList;
            OldCurrentSong = oldCurrentSong;
            NewCurrentSong = newCurrentSong;
        }
    }
}
