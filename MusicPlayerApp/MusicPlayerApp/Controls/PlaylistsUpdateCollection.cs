using MusicPlayer.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FolderMusic
{
    class PlaylistsUpdateCollection : ObservableCollection<IPlaylist>
    {
        private IPlaylistCollection source;

        public PlaylistsUpdateCollection(IPlaylistCollection source)
        {
            this.source = source;
            source.Changed += Source_Changed;

            Subscribe(source);
        }

        private void Source_Changed(object sender, PlaylistCollectionChangedEventArgs args)
        {
            Unsubscribe(args.GetRemoved());
            Subscribe(args.GetAdded());
        }

        private void Subscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                Subscribe(playlist);
            }
        }

        private void Unsubscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                Unsubscribe(playlist);
            }
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.Songs.Changed += OnPlaylistSongsChanged;
            Add(playlist);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.Songs.Changed -= OnPlaylistSongsChanged;
            Remove(playlist);
        }

        private void OnPlaylistSongsChanged(object sender, EventArgs args)
        {
            ISongCollection songs = (ISongCollection)sender;
            int index = source.IndexOf(songs.Parent);

            RemoveAt(index);
            Insert(index, songs.Parent);
        }
    }
}
