using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.ObjectModel;

namespace FolderMusic.Converters
{
    class ShuffleSongsCollection : ObservableCollection<Song>, IUpdateSelectedItemCollection<Song>
    {
        private IPlaylist source;

        public event EventHandler UpdateFinished;

        public ShuffleSongsCollection(IPlaylist source) : base()
        {
            this.source = source;

            source.SongsChanged += Source_SongsChanged;
            Subscribe(source.Songs);

            foreach (Song song in source.Songs) Add(song);
        }

        private void Subscribe(ISongCollection songs)
        {
            if (songs != null) songs.ShuffleChanged += Songs_ShuffleChanged;
        }

        private void Unsubscribe(ISongCollection songs)
        {
            if (songs != null) songs.ShuffleChanged += Songs_ShuffleChanged;
        }

        private void Subscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed += Shuffle_Changed;
        }

        private void Unsubscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed += Shuffle_Changed;
        }

        private void Subscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged += Song_Changed;
            song.TitleChanged += Song_Changed;
        }

        private void Unsubscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged += Song_Changed;
            song.TitleChanged += Song_Changed;
        }

        private void Source_SongsChanged(object sender, SongsChangedEventArgs e)
        {
            Unsubscribe(e.OldSongs);
            Subscribe(e.NewSongs);

            Clear();

            foreach (Song song in source.Songs.Shuffle) Add(song);

            UpdateFinished?.Invoke(this, EventArgs.Empty);
        }

        private void Songs_ShuffleChanged(object sender, ShuffleChangedEventArgs e)
        {
            Unsubscribe(e.OldShuffleSongs);
            Subscribe(e.NewShuffleSongs);

            Clear();

            foreach (Song song in source.Songs.Shuffle) Add(song);

            UpdateFinished?.Invoke(this, EventArgs.Empty);
        }

        private void Shuffle_Changed(object sender, ShuffleCollectionChangedEventArgs e)
        {
            foreach (Song song in e.GetRemoved()) Remove(song);
            foreach (ChangeCollectionItem<Song> change in e.AddedSongs) Insert(change.Index, change.Item);

            UpdateFinished?.Invoke(this, EventArgs.Empty);
        }

        private void Song_Changed(object sender, EventArgs e)
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
