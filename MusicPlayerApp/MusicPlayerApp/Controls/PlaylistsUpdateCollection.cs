using MusicPlayer.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FolderMusic
{
    class PlaylistsUpdateCollection : ObservableCollection<IPlaylist>, IUpdateSellectedItemCollection<IPlaylist>
    {
        private IPlaylistCollection source;

        public event UpdateFinishedEventHandler<IPlaylist> UpdateFinished;

        public PlaylistsUpdateCollection(IPlaylistCollection source)
        {
            this.source = source;
            source.Changed += Source_Changed;

            Subscribe(source);
            //MobileDebug.Manager.WriteEvent("PlaylistsUpdateCollectionConst", source.Count, Count);
        }

        private void Source_Changed(IPlaylistCollection sender, PlaylistCollectionChangedEventArgs args)
        {
            Unsubscribe(args.GetRemoved());
            Subscribe(args.GetAdded());

            UpdateFinished?.Invoke(this);
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

            playlist.Songs.Changed += OnPlaylistChanged;
            Add(playlist);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.Songs.Changed -= OnPlaylistChanged;
            Remove(playlist);
        }

        private void OnPlaylistChanged(ISongCollection sender, EventArgs args)
        {
            int index = source.IndexOf(sender.Parent);

            RemoveAt(index);
            Insert(index, sender.Parent);

            UpdateFinished?.Invoke(this);
        }
    }
}
