using PlayerIcons;
using System;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Generic;

namespace LibraryLib
{
    class ShuffleComplete : IShuffle
    {
        private const int shuffleCompleteListNextCount = 5, shuffleCompleteListPreviousCount = 3;
        Random ran = new Random();

        public List<int> AddSongsToShuffleList(List<int> shuffleList, List<Song> oldSongs, List<Song> updatedSongs)
        {
            for (int i = 0; i < shuffleList.Count; i++)
            {
                shuffleList[i] = GetUpdatedSongsIndexFromSongs(i, oldSongs, updatedSongs);
            }

            while (GetShuffleListIndex(shuffleList.Count) < GetShuffleListIndex(updatedSongs.Count))
            {
                AddRandomIndexToFrontOrBackOfShuffleList(ref shuffleList, updatedSongs.Count, true);
            }

            while (shuffleList.Count < GetShuffleListCount(updatedSongs.Count))
            {
                AddRandomIndexToFrontOrBackOfShuffleList(ref shuffleList, updatedSongs.Count, false);
            }

            return new List<int>(shuffleList);
        }

        private int GetUpdatedSongsIndexFromSongs(int songsIndex, List<Song> oldSongs, List<Song> updatedSongs)
        {
            return updatedSongs.IndexOf(oldSongs[songsIndex]);
        }

        public List<int> GenerateShuffleList(int songsIndex, int songsCount)
        {
            List<int> shuffleList = new List<int>(songsIndex);

            for (int i = 1; i < GetShuffleListCount(songsCount); i++)
            {
                AddRandomIndexToFrontOrBackOfShuffleList(ref shuffleList, songsCount, false);
            }

            return shuffleList;
        }

        public List<int> GetChangedShuffleListBecauseOfAnotherSongsIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
            AddSongsIndexToShuffleListAndRemoveFirst(songsIndex, ref shuffleList);

            while (true)
            {
                int offset = GetShuffleListIndex(shuffleList.Count) - shuffleList.IndexOf(songsIndex);

                if (offset == 0) return new List<int>(shuffleList);

                AddRandomIndexToFrontOrBackOfShuffleList(ref shuffleList, songsCount, offset > 0);
            }
        }

        private void AddSongsIndexToShuffleListAndRemoveFirst(int songsIndex, ref List<int> updatedShuffleList)
        {
            if (!updatedShuffleList.Contains(songsIndex))
            {
                updatedShuffleList.RemoveAt(0);
                updatedShuffleList.Add(songsIndex);
            }
        }

        public BitmapImage GetIcon()
        {
            try
            {
                return Icons.ShuffleComplete;
            }
            catch { }

            return new BitmapImage();
        }

        public ShuffleKind GetKind()
        {
            return ShuffleKind.Complete;
        }

        public IShuffle GetNext()
        {
            return new ShuffleOff();
        }

        public int GetShuffleListIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
            return GetShuffleListIndex(songsCount);
        }

        private int GetShuffleListIndex(int songsCount)
        {
            double indexDouble = (GetShuffleListCount(songsCount) - 1) /
                Convert.ToDouble(shuffleCompleteListNextCount + shuffleCompleteListPreviousCount) *
                shuffleCompleteListPreviousCount;

            return Convert.ToInt32(indexDouble);
        }

        public List<int> RemoveSongsIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
            int shuffleListIndex = shuffleList.IndexOf(songsIndex);

            if (shuffleListIndex != -1) return new List<int>(shuffleList);

            shuffleList.RemoveAt(shuffleListIndex);

            if (shuffleList.Count == songsCount) return new List<int>(shuffleList);

            bool front = GetShuffleListIndex(songsIndex, shuffleList, songsCount) >= songsIndex;
            AddRandomIndexToFrontOrBackOfShuffleList(ref shuffleList, songsCount, front);

            return new List<int>(shuffleList);
        }

        private void AddRandomIndexToFrontOrBackOfShuffleList(ref List<int> shuffleList, int songsCount, bool front)
        {
            if (front) shuffleList.Reverse();
            shuffleList.Add(GetRandomIndexWhichIsNotInShuffleList(shuffleList, songsCount));
            if (front) shuffleList.Reverse();
        }

        private int GetRandomIndexWhichIsNotInShuffleList(List<int> shuffleList,int maxExclusiv)
        {
            int index;

            do
            {
                index = ran.Next(maxExclusiv);

            } while (shuffleList.Contains(index));

            return index;
        }

        private int GetShuffleListCount(int songsCount)
        {
            int count = shuffleCompleteListNextCount + shuffleCompleteListPreviousCount + 1;
            return songsCount > count ? count : songsCount;
        }
    }
}
