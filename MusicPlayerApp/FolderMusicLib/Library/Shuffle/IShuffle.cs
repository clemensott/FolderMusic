using System.Collections.Generic;
using Windows.UI.Xaml.Media.Imaging;

namespace LibraryLib
{
    interface IShuffle
    {
        void AddSongsToShuffleList(ref List<int> shuffleList, List<Song> oldSongs, List<Song> updatedSongs);

        List<int> GenerateShuffleList(int songsIndex, int songsCount);

        void GetChangedShuffleListBecauseOfAnotherSongsIndex(int songsIndex, ref List<int> shuffleList, int songsCount);

        BitmapImage GetIcon();

        ShuffleKind GetKind();

        IShuffle GetNext();

        int GetShuffleListIndex(int songsIndex, List<int> shuffleList, int songsCount);

        void RemoveSongsIndex(int songsIndex, ref List<int> shuffleList, int songsCount);

        void CheckShuffleList(ref List<int> shuffleList, int songsCount);
    }
}
