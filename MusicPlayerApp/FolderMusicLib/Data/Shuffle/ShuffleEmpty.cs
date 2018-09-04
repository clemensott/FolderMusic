using System.Collections.Generic;
using Windows.UI.Xaml.Media.Imaging;

namespace MusicPlayer.Data.Shuffle
{
    class ShuffleEmpty : IShuffle
    {
        private const int shuffleCompleteListNextCount = 5, shuffleCompleteListPreviousCount = 3;
        private static IShuffle instance;

        public static IShuffle Instance
        {
            get
            {
                if (instance == null) instance = new ShuffleEmpty();

                return instance;
            }
        }

        private ShuffleEmpty() { }

        public void AddSongsToShuffleList(ref List<int> shuffleList, IList<Song> oldSongs, IList<Song> updatedSongs) { }

        public void CheckShuffleList(ref List<int> shuffleList, int songsCount) { }

        public List<int> GenerateShuffleList(int songsIndex, int songsCount)
        {
            return new List<int>() { 0 };
        }

        public void GetChangedShuffleListBecauseOfOtherSongsIndex(int songsIndex, ref List<int> shuffleList, int songsCount) { }

        public BitmapImage GetIcon()
        {
            return new BitmapImage();
        }

        public IShuffle GetNext()
        {
            return Instance;
        }

        public int GetShuffleListIndex(int songsIndex, List<int> shuffleList, int songsCount)
        {
          return  0;
        }

        public ShuffleType GetShuffleType()
        {
            return ShuffleType.Empty;
        }

        public void RemoveSongsIndex(int songsIndex, ref List<int> shuffleList, int songsCount) { }
    }
}
