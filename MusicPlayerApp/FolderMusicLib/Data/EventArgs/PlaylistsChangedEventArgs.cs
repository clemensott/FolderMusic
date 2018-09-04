using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data
{
    public class PlaylistsChangedEventArgs : EventArgs
    {
        public ChangedPlaylist[] AddedPlaylists { get; private set; }

        public ChangedPlaylist[] RemovedPlaylists { get; private set; }

        public IPlaylist OldCurrentPlaylist { get; private set; }

        public IPlaylist NewCurrentPlaylist { get; private set; }

        internal PlaylistsChangedEventArgs(ChangedPlaylist[] addPlaylists, ChangedPlaylist[] removePlaylists,
            IPlaylist oldCurrentPlaylist, IPlaylist newCurrentPlaylist)
        {
            AddedPlaylists = addPlaylists ?? new ChangedPlaylist[0];
            RemovedPlaylists = removePlaylists ?? new ChangedPlaylist[0];
            OldCurrentPlaylist = oldCurrentPlaylist;
            NewCurrentPlaylist = newCurrentPlaylist;
        }

        internal PlaylistsChangedEventArgs(IPlaylistCollection oldPlaylists, IPlaylistCollection newPlaylists,
            IPlaylist oldCurrentPlaylist, IPlaylist newCurrentPlaylist)
        {
            AddedPlaylists = newPlaylists?.Select((s, i) => new ChangedPlaylist(i, s)).
                Where(c => !oldPlaylists.Contains(c.Playlist)).ToArray() ?? new ChangedPlaylist[0];
            RemovedPlaylists = oldPlaylists?.Select((s, i) => new ChangedPlaylist(i, s)).
                Where(c => !newPlaylists.Contains(c.Playlist)).ToArray() ?? new ChangedPlaylist[0];

            OldCurrentPlaylist = oldCurrentPlaylist;
            NewCurrentPlaylist = newCurrentPlaylist;
        }

        public IEnumerable<IPlaylist> GetAdded()
        {
            return AddedPlaylists.Select(p => p.Playlist);
        }

        public IEnumerable<IPlaylist> GetRemoved()
        {
            return RemovedPlaylists.Select(p => p.Playlist);
        }
    }
}
