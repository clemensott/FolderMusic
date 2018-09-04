using PlayerIcons;
using System;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace LibraryLib
{
    class ShuffleOneTime : IShuffle
    {
        Random ran = new Random();

        public void AddSongsToShuffleList(ref List<int> shuffleList, List<Song> oldSongs, List<Song> updatedSongs)
        {
            List<int> newShuffleList = shuffleList.Where(x => updatedSongs.Contains(oldSongs[x])).
                Select(x => GetUpdatedSongsIndexFromSongs(x, oldSongs, updatedSongs)).ToList();

            var newSongs = updatedSongs.Where(x => !oldSongs.Contains(x));

            foreach (Song song in newSongs)
            {
                InsertSongAtRandomIndex(song, newShuffleList, updatedSongs);
            }

            shuffleList = newShuffleList;
        }

        private int GetUpdatedSongsIndexFromSongs(int songsIndex, List<Song> oldSongs, List<Song> updatedSongs)
        {
            return updatedSongs.IndexOf(oldSongs[songsIndex]);
        }

        private void InsertSongAtRandomIndex(Song song, List<int> shuffleList, List<Song> updatedSongs)
        {
            int songIndex = updatedSongs.IndexOf(song);
            int shuffleListIndex = ran.Next(shuffleList.Count);

            shuffleList.Insert(shuffleListIndex, songIndex);
        }

        public List<int> GenerateShuffleList(int songsIndex, int songsCount)
        {
            List<int> shuffleList = new List<int>() { songsIndex };

            for (int i = 1; i < songsCount; i++)
            {
                AddRandomIndexToShuffleList(shuffleList, songsCount);
            }

            return shuffleList;
        }

        private void AddRandomIndexToShuffleList(List<int> shuffleList, int songsCount)
        {
            shuffleList.Add(GetRandomIndexWhichIsNotInShuffleList(shuffleList, songsCount));
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

        public void GetChangedShuffleListBecauseOfAnotherSongsIndex
            (int songsIndex, ref List<int> shuffleList, int songsCount)
        {

        }

        public BitmapImage GetIcon()
        {
            try
            {
                return Icons.ShuffleOneTime;
            }
            catch { }

            return new BitmapImage();
        }

        public ShuffleKind GetKind()
        {
            return ShuffleKind.OneTime;
        }

        public IShuffle GetNext()
        {
            return new ShuffleComplete();
        }

        public int GetShuffleListIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
            return shuffleList.IndexOf(songsIndex);
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

            shuffleList = newShuffleList;
        }

        public void CheckShuffleList(ref List<int> shuffleList, int songsCount)
        {
            List<int> multipleIndexes, notIndexes;
            List<int> newShuffleList = new List<int>(shuffleList);

            GetMultipleAndNotIndexes(newShuffleList, songsCount, out multipleIndexes, out notIndexes);
            ChangeWrongIndexesOfShuffleList(newShuffleList, multipleIndexes, notIndexes);

            RemoveToHighIndexesInShuffleList(newShuffleList, songsCount);
            AddMissingIndexesToShuffleList(newShuffleList, songsCount);

            shuffleList = newShuffleList;
        }

        private void GetMultipleAndNotIndexes(List<int> shuffleList, int songsCount,
            out List<int> multipleIndexes, out List<int> notIndexes)
        {
            multipleIndexes = new List<int>();
            notIndexes = new List<int>();

            for (int i = 0; i < songsCount; i++)
            {
                int count = shuffleList.FindAll(x => x == i).Count;

                if (count < 1) notIndexes.Add(i);

                while (count > 1)
                {
                    multipleIndexes.Add(i);
                    count--;
                }
            }
        }

        private void ChangeWrongIndexesOfShuffleList(List<int> shuffleList,
            List<int> multipleIndexes, List<int> notIndexes)
        {
            foreach (int songsIndex in multipleIndexes)
            {
                int shuffleListIndex = shuffleList.IndexOf(songsIndex);

                if (notIndexes.Count > 0)
                {
                    shuffleList[shuffleListIndex] = notIndexes[0];
                    notIndexes.RemoveAt(0);
                }
                else shuffleList.RemoveAt(shuffleListIndex);
            }
        }

        private void RemoveToHighIndexesInShuffleList(List<int> shuffleList, int songsCount)
        {
            int[] toHighIndexes = shuffleList.Where(x => x >= songsCount).ToArray();

            foreach (int toHighIndex in toHighIndexes)
            {
                shuffleList.Remove(toHighIndex);
            }
        }

        private void AddMissingIndexesToShuffleList(List<int> shuffleList, int songsCount)
        {
            while (shuffleList.Count < songsCount)
            {
                AddRandomIndexToShuffleList(shuffleList, songsCount);
            }
        }
    }
}
