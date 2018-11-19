using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data.Shuffle
{
    class ShuffleOneTimeCollection : ShuffleCollectionBase
    {
        private static Random ran = new Random();

        public ShuffleOneTimeCollection(ISongCollection parent) : this(parent, Enumerable.Empty<Song>())
        {
        }

        public ShuffleOneTimeCollection(ISongCollection parent, Song currentSong) : this(parent, GetStart(parent, currentSong))
        {
        }

        public ShuffleOneTimeCollection(ISongCollection parent, IEnumerable<Song> songs) : base(parent)
        {
            parent.Changed += Parent_CollectionChanged;

            Change(null, songs);
        }

        private void Parent_CollectionChanged(object sender, SongCollectionChangedEventArgs e)
        {
            List<ChangeCollectionItem<Song>> adds = new List<ChangeCollectionItem<Song>>();

            foreach (Song addSong in e.GetAdded())
            {
                int index = ran.Next(Count - e.RemovedSongs.Length + adds.Count + 1);

                adds.Add(new ChangeCollectionItem<Song>(index, addSong));
            }

            Change(e.GetRemoved(), adds);
        }

        protected override ShuffleType GetShuffleType()
        {
            return ShuffleType.OneTime;
        }

        protected static IEnumerable<Song> GetStart(ISongCollection songs, Song currentSong)
        {
            List<Song> remaining = new List<Song>(songs);

            if (currentSong != null && remaining.Remove(currentSong)) yield return currentSong;

            while (remaining.Count > 0)
            {
                int index = ran.Next(remaining.Count);
                yield return remaining[index];

                remaining.RemoveAt(index);
            }
        }

        protected override IShuffleCollection GetNewThis(IEnumerable<Song> songs)
        {
            return new ShuffleOneTimeCollection(Parent, songs);
        }
    }
}
