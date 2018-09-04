using MusicPlayer.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FolderMusic.Converters
{
    class ShuffleOffSongsCollection : ObservableCollection<Song>, IUpdateSellectedItemCollection<Song>
    {
        private ISongCollection source;

        public event UpdateFinishedEventHandler<Song> UpdateFinished;

        public ShuffleOffSongsCollection(IPlaylist playlist)
        {
            source = playlist.Songs;
            source.Changed += Source_Changed;

            Subscribe(source);
        }

        private void Source_Changed(ISongCollection sender, SongCollectionChangedEventArgs args)
        {
            Unsubscribe(args.GetRemoved());
            Subscribe(args.GetAdded());

            UpdateFinished?.Invoke(this);
        }

        private void Subscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>())
            {
                Subscribe(song);
            }
        }

        private void Unsubscribe(IEnumerable<Song> songs)
        {
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

            int index = this.Count(s => Compare(s, song) < 0);
            Insert(index, song);
        }

        private int Compare(Song s0, Song s1)
        {
            int titleCompare = s0.Title.CompareTo(s1.Title);

            if (titleCompare != 0) return titleCompare;

            return s0.Artist.CompareTo(s0.Artist);
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
            int oldIndex = source.IndexOf(sender);
            int newIndex = this.Count(s => Compare(s, sender) < 0);

            Move(oldIndex, newIndex);

            UpdateFinished?.Invoke(this);
        }
    }
}
