﻿using System;

namespace MusicPlayer.Data
{
    public class PlaylistsChangedEventArgs : EventArgs
    {
        public IPlaylistCollection OldPlaylists { get; private set; }

        public IPlaylistCollection NewPlaylists { get; private set; }

        public IPlaylist OldCurrentPlaylist { get; private set; }

        public IPlaylist NewCurrentPlaylist { get; private set; }

        public PlaylistsChangedEventArgs(IPlaylistCollection oldPlaylists,
            IPlaylistCollection newPlaylists, IPlaylist oldCurrentPlaylist, IPlaylist newCurrentPlaylist)
        {
            OldPlaylists = oldPlaylists;
            NewPlaylists = newPlaylists;

            OldCurrentPlaylist = oldCurrentPlaylist;
            NewCurrentPlaylist = newCurrentPlaylist;
        }
    }
}