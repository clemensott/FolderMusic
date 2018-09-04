using PlayerIcons;
using System;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Generic;

namespace LibraryLib
{
    class ShuffleOff : IShuffle
    {
        public List<int> AddSongsToShuffleList(List<int> shuffleList, List<Song> oldSongs, List<Song> updatedSongs)
        {
            return GenerateShuffleList(0, updatedSongs.Count);
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

        public List<int> GetChangedShuffleListBecauseOfAnotherSongsIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
            return shuffleList;
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
            return shuffleList.IndexOf(songsIndex);
        }

        public List<int> RemoveSongsIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
            if (shuffleList.Contains(songsIndex)) shuffleList.RemoveAt(shuffleList.Count - 1);

            return new List<int>(shuffleList);
        }
    }
}
