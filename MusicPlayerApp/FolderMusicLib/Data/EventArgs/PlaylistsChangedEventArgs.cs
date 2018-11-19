using System;

namespace MusicPlayer.Data
{
    public class PlaylistsChangedEventArgs : EventArgs
    {
        public IPlaylistCollection OldPlaylists { get; private set; }

        public IPlaylistCollection NewPlaylists { get; private set; }

        public PlaylistsChangedEventArgs(IPlaylistCollection oldPlaylists, IPlaylistCollection newPlaylists)
        {
            OldPlaylists = oldPlaylists;
            NewPlaylists = newPlaylists;
        }
    }
}