using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FolderMusic.Converters
{
    class ShuffleSongsCollection : ObservableCollection<Song>, IUpdateSellectedItemCollection<Song>
    {
        private IPlaylist source;

        public event UpdateFinishedEventHandler<Song> UpdateFinished;

        public ShuffleSongsCollection(IPlaylist source) : base()
        {
            this.source = source;
            if (source == null) return;

            source.ShuffleChanged += Source_ShuffleChanged;
            source.ShuffleSongs.Changed += ShuffleCollection_Changed;

            Subscribe(source.ShuffleSongs);
        }

        private void Source_ShuffleChanged(IPlaylist sender, ShuffleChangedEventArgs args)
        {
            Unsubscribe(this.ToList());
            Subscribe(source.ShuffleSongs);

            UpdateFinished?.Invoke(this);
        }

        private void ShuffleCollection_Changed(IShuffleCollection sender)
        {
            Unsubscribe(this.ToList());
            Subscribe(source.ShuffleSongs);

            UpdateFinished?.Invoke(this);
        }

        private void Subscribe(IEnumerable<Song> songs)
        {
            if (songs == null) return;

            foreach (Song song in songs ?? Enumerable.Empty<Song>())
            {
                Subscribe(song);
            }
        }

        private void Unsubscribe(IEnumerable<Song> songs)
        {
            if (songs == null) return;

            foreach (Song song in songs ?? Enumerable.Empty<Song>())
            {
                Unsubscribe(song);
            }
        }

        private void Subscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged += OnSongChanged;
            song.TitleChanged += OnSongChanged;

            Add(song);
        }

        private void Unsubscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged -= OnSongChanged;
            song.TitleChanged -= OnSongChanged;

            Remove(song);
        }

        private void OnSongChanged(Song sender, EventArgs args)
        {
            int index = source.ShuffleSongs.IndexOf(sender);

            RemoveAt(index);
            Insert(index, sender);

            UpdateFinished?.Invoke(this);
        }
    }
}
