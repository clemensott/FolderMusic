using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FolderMusic.Converters
{
    class ShuffleSongsCollection : ObservableCollection<Song>, IUpdateSelectedItemCollection<Song>
    {
        private ISongCollection source;

        public event EventHandler UpdateFinished;

        public ShuffleSongsCollection(ISongCollection songs) : base()
        {
            source = songs;

            if (source == null) return;

            source.ShuffleChanged += Songs_ShuffleChanged;

            Subscribe(source.Shuffle);

            foreach (Song song in source.Shuffle) Add(song);
        }

        private void Songs_ShuffleChanged(object sender, ShuffleChangedEventArgs e)
        {
            Unsubscribe(e.OldShuffleSongs);
            Subscribe(e.NewShuffleSongs);

            Clear();

            foreach (Song song in source.Shuffle) Add(song);

            UpdateFinished?.Invoke(this, EventArgs.Empty);
        }

        private void Subscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed += OnShuffleCollectionChanged;
        }

        private void Unsubscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed += OnShuffleCollectionChanged;
        }

        private void Subscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged += OnSongChanged;
            song.TitleChanged += OnSongChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged += OnSongChanged;
            song.TitleChanged += OnSongChanged;
        }

        private void OnShuffleCollectionChanged(object sender, ShuffleCollectionChangedEventArgs e)
        {
            foreach (Song song in e.GetRemoved()) Remove(song);
            foreach (ChangeCollectionItem<Song> change in e.AddedSongs.OrderBy(c => c.Index)) Insert(change.Index, change.Item);

            UpdateFinished?.Invoke(this, EventArgs.Empty);
        }

        private void OnSongChanged(object sender, EventArgs e)
        {
            int index = IndexOf((Song)sender);

            RemoveAt(index);
            Insert(index, (Song)sender);
        }

        protected override void ClearItems()
        {
            foreach (Song song in this) Unsubscribe(song);

            base.ClearItems();
        }

        protected override void InsertItem(int index, Song item)
        {
            Subscribe(item);

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
