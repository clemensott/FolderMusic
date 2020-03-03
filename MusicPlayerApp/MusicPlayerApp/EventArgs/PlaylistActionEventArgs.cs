using System;
using MusicPlayer.Models.Interfaces;

namespace FolderMusic
{
    public class PlaylistActionEventArgs : EventArgs
    {
        public IPlaylist Playlist { get; private set; }

        public PlaylistActionEventArgs(IPlaylist playlist)
        {
            Playlist = playlist;
        }
    }
}
