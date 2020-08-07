using System;
using MusicPlayer.Models.Interfaces;

namespace FolderMusic
{
    public class PlaylistActionEventArgs : System.EventArgs
    {
        public IPlaylist Playlist { get; }

        public PlaylistActionEventArgs(IPlaylist playlist)
        {
            Playlist = playlist;
        }
    }
}
