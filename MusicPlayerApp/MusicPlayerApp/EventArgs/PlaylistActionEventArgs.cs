using MusicPlayer.Data;
using System;

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
