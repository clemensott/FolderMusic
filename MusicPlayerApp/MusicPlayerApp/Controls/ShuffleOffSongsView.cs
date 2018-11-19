﻿using System;
using FolderMusic.Converters;
using MusicPlayer.Data;
using Windows.UI.Xaml.Data;

namespace FolderMusic
{
    class ShuffleOffSongsView : SongsView
    {
        protected override IUpdateSelectedItemCollection<Song> GetItemsSource(IPlaylist playlist)
        {
            return new ShuffleOffSongsCollection(playlist);
        }
    }
}
