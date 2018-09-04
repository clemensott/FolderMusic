using System.Collections.Generic;
using Windows.UI.Xaml.Media.Imaging;

namespace LibraryLib
{
    interface IShuffle
    {
        List<int> AddSongsToShuffleList(List<int> shuffleList, List<Song> oldSongs, List<Song> updatedSongs);

        List<int> GenerateShuffleList(int songsIndex, int songsCount);

        List<int> GetChangedShuffleListBecauseOfAnotherSongsIndex(int songsIndex, List<int> shuffleList, int songsCount);

        BitmapImage GetIcon();

        ShuffleKind GetKind();

        IShuffle GetNext();

        int GetShuffleListIndex(int songsIndex, List<int> shuffleList, int songsCount);

        List<int> RemoveSongsIndex(int songsIndex, List<int> shuffleList, int songsCount);
    }
}
