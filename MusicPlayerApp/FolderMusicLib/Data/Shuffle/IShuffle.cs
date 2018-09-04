using System.Collections.Generic;

namespace MusicPlayer.Data.Shuffle
{
    public enum ShuffleType { Off, OneTime, Complete, Empty };

    interface IShuffle
    {
        void AddSongsToShuffleList(ref List<int> shuffleList, IList<Song> oldSongs, IList<Song> updatedSongs);

        List<int> GenerateShuffleList(int songsIndex, int songsCount);

        void GetChangedShuffleListBecauseOfOtherSongsIndex(int songsIndex, ref List<int> shuffleList, int songsCount);

        ShuffleType GetShuffleType();

        IShuffle GetNext();

        int GetShuffleListIndex(int songsIndex, List<int> shuffleList, int songsCount);

        void RemoveSongsIndex(int songsIndex, ref List<int> shuffleList, int songsCount);

        void CheckShuffleList(ref List<int> shuffleList, int songsCount);
    }
}
