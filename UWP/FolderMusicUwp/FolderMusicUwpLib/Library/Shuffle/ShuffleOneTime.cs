using System;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using FolderMusicUwpLib;

namespace LibraryLib
{
    class ShuffleOneTime : IShuffle
    {
        Random ran = new Random();

        public List<int> AddSongsToShuffleList(List<int> shuffleList, List<Song> oldSongs, List<Song> updatedSongs)
        {
            for (int i = shuffleList.Count - 1; i >= 0; i--)
            {
                if (updatedSongs.Contains(oldSongs[shuffleList[i]]))
                {
                    shuffleList[i] = GetUpdatedSongsIndexFromSongs(shuffleList[i], oldSongs, updatedSongs);
                }
                else shuffleList.RemoveAt(i);
            }

            for (int i = shuffleList.Count; i < updatedSongs.Count; i++)
            {
                AddRandomIndexToFrontOrBackOfShuffleList(ref shuffleList, updatedSongs.Count);
            }

            return new List<int>(shuffleList);
        }

        private int GetUpdatedSongsIndexFromSongs(int songsIndex, List<Song> oldSongs, List<Song> updatedSongs)
        {
            return updatedSongs.IndexOf(oldSongs[songsIndex]);
        }

        public List<int> GenerateShuffleList(int songsIndex,int songsCount)
        {
            List<int> shuffleList = new List<int>() { songsIndex };

            for (int i = 1; i < songsCount; i++)
            {
                AddRandomIndexToFrontOrBackOfShuffleList(ref shuffleList, songsCount);
            }

            return shuffleList;
        }

        private void AddRandomIndexToFrontOrBackOfShuffleList(ref List<int> shuffleList, int songsCount)
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

        public List<int> GetChangedShuffleListBecauseOfAnotherSongsIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
            return shuffleList;
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

        public List<int> RemoveSongsIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
            int shuffleListIndex = shuffleList.IndexOf(songsIndex);

            if (shuffleListIndex != -1)
            {
                shuffleList.RemoveAt(shuffleListIndex);

                for (int i = 0; i < shuffleList.Count; i++)
                {
                    if (shuffleList[i] > songsIndex) shuffleList[i]--;
                }
            }

            return new List<int>(shuffleList);
        }
    }
}
