using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data.Shuffle
{
    class ShuffleOffCollection : ShuffleCollectionBase
    {
        public ShuffleOffCollection(ISongCollection parent) : this(parent, GetStart(parent))
        {
        }

        public ShuffleOffCollection(ISongCollection parent, IEnumerable<Song> shuffleSongs) : base(parent)
        {
            Changed += OnChanged;
            parent.Changed += Parent_CollectionChanged;

            Change(null, shuffleSongs);
        }

        private static IEnumerable<Song> GetStart(ISongCollection songs)
        {
            return songs.OrderBy(s => s.Title).ThenBy(s => s.Artist);
        }

        private void OnChanged(object sender, ShuffleCollectionChangedEventArgs e)
        {
            foreach (Song song in e.GetRemoved()) Unsubscribe(song);
            foreach (Song song in e.GetAdded()) Subscribe(song);
        }

        private void Parent_CollectionChanged(object sender, SongCollectionChangedEventArgs e)
        {
            Song[] ordered = GetOrdered(Parent).ToArray();

            Change(e.GetRemoved(), e.GetAdded().Select(s => new ChangeCollectionItem<Song>(Array.IndexOf(ordered, s), s)));
        }

        private void Subscribe(Song song)
        {
            song.TitleChanged += OnSongChanged;
            song.ArtistChanged += OnSongChanged;
        }

        private void Unsubscribe(Song song)
        {
            song.TitleChanged -= OnSongChanged;
            song.ArtistChanged -= OnSongChanged;
        }

        private void OnSongChanged(object sender, EventArgs args)
        {
            Song song = (Song)sender;
            int oldIndex = IndexOf(this, song);
            int newIndex = IndexOf(GetOrdered(this), song);

            if (oldIndex != newIndex) Move(song, newIndex);
        }

        private int IndexOf(IEnumerable<Song> songs, Song searchSong)
        {
            int index = 0;

            foreach (Song song in GetOrdered(this))
            {
                if (ReferenceEquals(song, searchSong)) return index;

                index++;
            }

            return -1;
        }

        private void Move(Song song, int newIndex)
        {
            Change(new Song[] { song }, new ChangeCollectionItem<Song>[] { new ChangeCollectionItem<Song>(newIndex, song) });
        }

        protected override ShuffleType GetShuffleType()
        {
            return ShuffleType.Off;
        }

        private void UpdateOrder()
        {
            Song[] orderedArray = GetOrdered(this).ToArray();
            List<Song> removes = new List<Song>();
            List<ChangeCollectionItem<Song>> adds = new List<ChangeCollectionItem<Song>>();

            for (int i = 0; i < orderedArray.Length; i++)
            {
                int currentIndex = IndexOf(orderedArray[i]);

                if (i == currentIndex) continue;

                Song moveSong = orderedArray[i];
                removes.Add(moveSong);
                adds.Add(new ChangeCollectionItem<Song>(i, moveSong));
            }

            if (removes.Any()) Change(removes, adds);
        }

        private static IOrderedEnumerable<Song> GetOrdered(IEnumerable<Song> songs)
        {
            return songs.OrderBy(s => s.Title).ThenBy(s => s.Artist);
        }

        protected override IShuffleCollection GetNewThis(IEnumerable<Song> songs)
        {
            return new ShuffleOffCollection(Parent, songs);
        }
    }
}
