using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data.Shuffle
{
    class ShuffleComplete : IShuffle
    {
        private const int shuffleCompleteListNextCount = 5, shuffleCompleteListPreviousCount = 3;
        private static IShuffle instance;

        public static IShuffle Instance
        {
            get
            {
                if (instance == null) instance = new ShuffleComplete();

                return instance;
            }
        }

        private Random ran = new Random();

        private ShuffleComplete() { }

        public void AddSongsToShuffleList(ref List<int> shuffleList, IList<Song> oldSongs, IList<Song> updatedSongs)
        {
            List<int> newShuffleList = shuffleList.Where(x => updatedSongs.Contains(oldSongs[x])).
              Select(x => GetUpdatedSongsIndexFromSongs(x, oldSongs, updatedSongs)).ToList();

            while (GetShuffleListIndex(newShuffleList.Count) < GetShuffleListIndex(updatedSongs.Count))
            {
                AddRandomIndexToFrontOrBackOfShuffleList(newShuffleList, updatedSongs.Count, true, false);
            }

            AddIndexesToShuffleListIfToLess(newShuffleList, updatedSongs.Count);

            shuffleList = newShuffleList;
        }

        private int GetUpdatedSongsIndexFromSongs(int songsIndex, IList<Song> oldSongs, IList<Song> updatedSongs)
        {
            return updatedSongs.IndexOf(oldSongs[songsIndex]);
        }

        private void AddIndexesToShuffleListIfToLess(List<int> shuffleList, int songsCount)
        {
            while (shuffleList.Count < GetShuffleListCount(songsCount))
            {
                bool addToFront = WouldSameShuffleListIndex(shuffleList.Count, shuffleList.Count + 1);

                AddRandomIndexToFrontOrBackOfShuffleList(shuffleList, songsCount, addToFront, false);
            }
        }

        private bool WouldSameShuffleListIndex(int count1, int count2)
        {
            return GetShuffleListIndex(count1) == GetShuffleListIndex(count2);
        }

        public List<int> GenerateShuffleList(int songsIndex, int songsCount)
        {
            List<int> shuffleList = new List<int>();

            for (int i = 0; i < GetShuffleListCount(songsCount); i++)
            {
                if (i == GetShuffleListIndex(songsCount)) shuffleList.Add(songsIndex);
                else AddRandomIndexToFrontOrBackOfShuffleList(shuffleList, songsCount, false, false);
            }

            return shuffleList;
        }

        public void GetChangedShuffleListBecauseOfOtherSongsIndex(int songsIndex, ref List<int> shuffleList, int songsCount)
        {
            List<int> newShuffleList = new List<int>(shuffleList);

            AddSongsIndexToShuffleListAndRemoveFirst(songsIndex, newShuffleList);

            while (true)
            {
                int offset = GetShuffleListIndex(newShuffleList.Count) - newShuffleList.IndexOf(songsIndex);

                if (offset == 0)
                {
                    shuffleList = newShuffleList;
                    return;
                }

                AddRandomIndexToFrontOrBackOfShuffleList(newShuffleList, songsCount, offset > 0, true);
            }
        }

        private void AddSongsIndexToShuffleListAndRemoveFirst(int songsIndex, List<int> updatedShuffleList)
        {
            if (!updatedShuffleList.Contains(songsIndex))
            {
                updatedShuffleList.RemoveAt(0);
                updatedShuffleList.Add(songsIndex);
            }
        }

        public ShuffleType GetShuffleType()
        {
            return ShuffleType.Complete;
        }

        public IShuffle GetNext()
        {
            return ShuffleOff.Instance;
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

        public void RemoveSongsIndex(int songsIndex, ref List<int> shuffleList, int songsCount)
        {
            List<int> newShuffleList = new List<int>(shuffleList);
            int shuffleListIndex = newShuffleList.IndexOf(songsIndex);

            if (shuffleListIndex == -1) return;

            newShuffleList.RemoveAt(shuffleListIndex);

            for (int i = 0; i < newShuffleList.Count; i++)
            {
                if (newShuffleList[i] > songsIndex) newShuffleList[i]--;
            }

            while (newShuffleList.Count < GetShuffleListCount(songsCount))
            {
                bool front = GetShuffleListIndex(songsIndex, newShuffleList, songsCount) >= shuffleListIndex;

                AddRandomIndexToFrontOrBackOfShuffleList(newShuffleList, songsCount, front, false);
            }

            shuffleList = newShuffleList;
        }

        private void AddRandomIndexToFrontOrBackOfShuffleList(List<int> shuffleList, int songsCount, bool front, bool keepCount)
        {
            int index = GetRandomIndexWhichIsNotInShuffleList(shuffleList, songsCount);

            if (keepCount) shuffleList.RemoveAt(0);

            if (front) shuffleList.Insert(0, index);
            else shuffleList.Add(index);
        }

        private int GetRandomIndexWhichIsNotInShuffleList(List<int> shuffleList, int maxExclusiv)
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

        public void CheckShuffleList(ref List<int> shuffleList, int songsCount)
        {
            List<int> newShuffleList = new List<int>(shuffleList);

            for (int i = 0; i < newShuffleList.Count; i++)
            {
                List<int> multipleIndexes = GetMultipleIndexes(newShuffleList, i);

                while (multipleIndexes.Count > 0)
                {
                    int shuffleListIndex = multipleIndexes[0] == GetShuffleListIndex(newShuffleList.Count) ?
                        i : multipleIndexes[0];

                    ReplaceIndexInShuffleListWithRandom(newShuffleList, shuffleListIndex, songsCount);
                }
            }

            AddIndexesToShuffleListIfToLess(newShuffleList, songsCount);
            RemoveIndexesFromShuffleListIfToMany(newShuffleList, songsCount);

            shuffleList = newShuffleList;
        }

        private List<int> GetMultipleIndexes(List<int> shuffleList, int shuffleListIndex)
        {
            List<int> multipleIndexes = new List<int>();

            for (int j = shuffleListIndex + 1; j < shuffleList.Count; j++)
            {
                if (shuffleList[shuffleListIndex] == shuffleList[j]) multipleIndexes.Add(j);
            }

            return multipleIndexes;
        }

        private void ReplaceIndexInShuffleListWithRandom(List<int> shuffleList, int shuffleListIndex, int songsCount)
        {
            shuffleList[shuffleListIndex] = GetRandomIndexWhichIsNotInShuffleList(shuffleList, songsCount);
        }

        private void RemoveIndexesFromShuffleListIfToMany(List<int> shuffleList, int songsCount)
        {
            while (GetShuffleListCount(shuffleList.Count) != GetShuffleListCount(songsCount))
            {
                bool removeAtBack = WouldSameShuffleListIndex(shuffleList.Count, songsCount);

                shuffleList.RemoveAt(removeAtBack ? shuffleList.Count - 1 : 0);
            }
        }
    }
}
