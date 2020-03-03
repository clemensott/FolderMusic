using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models.Shuffle
{
    class ShuffleOffCollection : ShuffleCollectionBase
    {
        public ShuffleOffCollection(ISongCollection parent) : this(parent, GetOrdered(parent))
        {
        }

        private ShuffleOffCollection(ISongCollection parent, IEnumerable<Song> shuffleSongs) : base(parent)
        {
            Changed += OnChanged;
            parent.Changed += Parent_CollectionChanged;

            Change(null, shuffleSongs);
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

        private void OnSongChanged(object sender, System.EventArgs args)
        {
            Song song = (Song)sender;
            int oldIndex = IndexOf(this, song);
            int newIndex = IndexOf(GetOrdered(this), song);

            if (oldIndex != newIndex) Move(song, newIndex);
        }

        private static int IndexOf(IEnumerable<Song> songs, Song searchSong)
        {
            int index = 0;

            foreach (Song song in GetOrdered(songs))
            {
                if (ReferenceEquals(song, searchSong)) return index;

                index++;
            }

            return -1;
        }

        private void Move(Song song, int newIndex)
        {
            Change(new[] { song }, new[] { new ChangeCollectionItem<Song>(newIndex, song) });
        }

        protected override ShuffleType GetShuffleType()
        {
            return ShuffleType.Off;
        }

        protected override void UpdateCurrentSong(Song[] oldShuffle)
        {
            Song currentSong = Parent.Parent.CurrentSong;

            if (currentSong != null)
            {
                IEnumerable<Song> shuffleWithCurrentSong = this.Concat(Enumerable.Repeat(currentSong, 1));
                int index = GetOrdered(shuffleWithCurrentSong).IndexOf(currentSong) % Count;

                Parent.Parent.CurrentSong = this.ElementAt(index);
            }
            else Parent.Parent.CurrentSong = this.FirstOrDefault();
        }

        private static IOrderedEnumerable<Song> GetOrdered(IEnumerable<Song> songs)
        {
            return songs.OrderBy(s => s.Title).ThenBy(s => s.Artist);
        }

        protected override IShuffleCollection GetNewThis(IEnumerable<Song> songs)
        {
            return new ShuffleOffCollection(Parent, songs);
        }

        public override void Dispose()
        {
            Changed -= OnChanged;

            if (Parent != null) Parent.Changed -= Parent_CollectionChanged;

            foreach (Song song in this) Unsubscribe(song);
        }
    }
}
