using System;
using FolderMusic.Converters;
using MusicPlayer.Data;
using Windows.UI.Xaml.Data;

namespace FolderMusic
{
    class ShuffleSongsView : SongsView
    {
        protected override IUpdateSelectedItemCollection<Song> GetItemsSource(ISongCollection songs)
        {
            return new ShuffleSongsCollection(songs);
        }
    }
}
