using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data
{
    public class PlaylistCollectionChangedEventArgs : EventArgs
    {
        public ChangeCollectionItem<IPlaylist>[] AddedPlaylists { get; private set; }

        public ChangeCollectionItem<IPlaylist>[] RemovedPlaylists { get; private set; }

        internal PlaylistCollectionChangedEventArgs(ChangeCollectionItem<IPlaylist>[] addPlaylists,
            ChangeCollectionItem<IPlaylist>[] removePlaylists)
        {
            AddedPlaylists = addPlaylists ?? new ChangeCollectionItem<IPlaylist>[0];
            RemovedPlaylists = removePlaylists ?? new ChangeCollectionItem<IPlaylist>[0];
        }

        internal PlaylistCollectionChangedEventArgs(IPlaylistCollection oldPlaylists, IPlaylistCollection newPlaylists)
        {
            AddedPlaylists = newPlaylists?.Select((s, i) => new ChangeCollectionItem<IPlaylist>(i, s)).
                Where(c => !oldPlaylists.Contains(c.Item)).ToArray() ?? new ChangeCollectionItem<IPlaylist>[0];
            RemovedPlaylists = oldPlaylists?.Select((s, i) => new ChangeCollectionItem<IPlaylist>(i, s)).
                Where(c => !newPlaylists.Contains(c.Item)).ToArray() ?? new ChangeCollectionItem<IPlaylist>[0];
        }

        public IEnumerable<IPlaylist> GetAdded()
        {
            return AddedPlaylists.Select(p => p.Item);
        }

        public IEnumerable<IPlaylist> GetRemoved()
        {
            return RemovedPlaylists.Select(p => p.Item);
        }
    }
}
