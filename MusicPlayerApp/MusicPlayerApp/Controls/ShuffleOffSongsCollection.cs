using MusicPlayer.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FolderMusic.Converters
{
    class ShuffleOffSongsCollection : ObservableCollection<Song>, IUpdateSelectedItemCollection<Song>
    {
        private ISongCollection source;

        public event EventHandler UpdateFinished;

        public ShuffleOffSongsCollection(ISongCollection songs)
        {
            source = songs;
            source.Changed += OnSourceChanged;

            foreach (Song song in source) Add(song);
        }

        private void Subscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged += OnSongChanged;
            song.TitleChanged += OnSongChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged -= OnSongChanged;
            song.TitleChanged -= OnSongChanged;
        }

        private void OnSourceChanged(object sender, SongCollectionChangedEventArgs e)
        {
            foreach (Song song in e.GetRemoved()) Remove(song);
            foreach (Song song in e.GetAdded()) Add(song);

            UpdateFinished?.Invoke(this, EventArgs.Empty);
        }

        private void OnSongChanged(object sender, EventArgs args)
        {
            Song song = (Song)sender;
            int oldIndex = IndexOf(song);
            int newIndex = this.Count(s => Compare(s, song) < 0);

            Move(oldIndex, newIndex);

            UpdateFinished?.Invoke(this, EventArgs.Empty);
        }

        private static int Compare(Song s0, Song s1)
        {
            int titleCompare = s0.Title.CompareTo(s1.Title);

            if (titleCompare != 0) return titleCompare;

            return s0.Artist.CompareTo(s0.Artist);
        }

        protected override void ClearItems()
        {
            foreach (Song song in this) Unsubscribe(song);

            base.ClearItems();
        }

        protected override void InsertItem(int index, Song item)
        {
            index = this.Count(s => Compare(s, item) < 0);

            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            Unsubscribe(this[index]);

            base.RemoveItem(index);
        }

        protected override void SetItem(int index, Song item)
        {
            Unsubscribe(this[index]);
            Subscribe(item);

            base.SetItem(index, item);
        }
    }
}
