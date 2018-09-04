using PlayerIcons;
using System;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Generic;

namespace LibraryLib
{
    class ShuffleOff : IShuffle
    {
        public void AddSongsToShuffleList(ref List<int> shuffleList, List<Song> oldSongs, List<Song> updatedSongs)
        {
            shuffleList = GenerateShuffleList(0, updatedSongs.Count);
        }

        public List<int> GenerateShuffleList(int songsIndex, int songsCount)
        {
            List<int> shuffleList = new List<int>();

            for (int i = 0; i < songsCount; i++)
            {
                shuffleList.Add(i);
            }

            return shuffleList;
        }

        public void GetChangedShuffleListBecauseOfAnotherSongsIndex
            (int songsIndex, ref List<int> shuffleList, int songsCount)
        {

        }

        public BitmapImage GetIcon()
        {
            try
            {
                return Icons.ShuffleOff;
            }
            catch { }

            return new BitmapImage();
        }

        public ShuffleKind GetKind()
        {
            return ShuffleKind.Off;
        }

        public IShuffle GetNext()
        {
            return new ShuffleOneTime();
        }

        public int GetShuffleListIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
            return songsIndex;
        }

        public void RemoveSongsIndex(int songsIndex, ref List<int> shuffleList, int songsCount)
        {
            List<int> newShuffleList = new List<int>(shuffleList);

            if (newShuffleList.Contains(songsIndex)) newShuffleList.RemoveAt(newShuffleList.Count - 1);

            shuffleList = newShuffleList;
        }

        public void CheckShuffleList(ref List<int> shuffleList, int songsCount)
        {
            shuffleList = GenerateShuffleList(0, songsCount);
        }
    }
}
