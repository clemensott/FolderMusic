using System;
using FolderMusic.Converters;
using MusicPlayer.Data;
using Windows.UI.Xaml.Data;

namespace FolderMusic
{
    class ShuffleSongsView : SongsView
    {
        protected override IUpdateSellectedItemCollection<Song> GetItemsSource(IPlaylist playlist)
        {
            return new ShuffleSongsCollection(playlist);
        }
    }
}
