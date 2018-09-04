using System;
using System.Linq;

namespace MusicPlayer.Data
{
    public enum ChangeType { Add, Remoce }

    public class PlaylistsChangedEventArgs : EventArgs
    {
        public ChangedPlaylist[] AddPlaylists { get; private set; }

        public ChangedPlaylist[] RemovePlaylists { get; private set; }

        public Playlist OldCurrentPlaylist { get; private set; }

        public Playlist NewCurrentPlaylist { get; private set; }

        internal PlaylistsChangedEventArgs(ChangedPlaylist[] addPlaylists, ChangedPlaylist[] removePlaylists,
            Playlist oldCurrentPlaylist, Playlist newCurrentPlaylist)
        {
            AddPlaylists = addPlaylists;
            RemovePlaylists = removePlaylists;
            OldCurrentPlaylist = oldCurrentPlaylist;
            NewCurrentPlaylist = newCurrentPlaylist;
        }

        internal PlaylistsChangedEventArgs(PlaylistList oldPlaylists, PlaylistList newPlaylists,
            Playlist oldCurrentPlaylist, Playlist newCurrentPlaylist)
        {
            AddPlaylists = newPlaylists.Select((s, i) => new ChangedPlaylist(i, s)).
                Where(c => !oldPlaylists.Contains(c.Playlist)).ToArray();
            RemovePlaylists = oldPlaylists.Select((s, i) => new ChangedPlaylist(i, s)).
                Where(c => !newPlaylists.Contains(c.Playlist)).ToArray();

            OldCurrentPlaylist = oldCurrentPlaylist;
            NewCurrentPlaylist = newCurrentPlaylist;
        }
    }
}
