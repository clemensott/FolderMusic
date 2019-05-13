using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data.Shuffle
{
    class ShufflePathCollection : ShuffleCollectionBase
    {
        public ShufflePathCollection(ISongCollection parent) : this(parent, GetOrdered(parent))
        {
        }

        private ShufflePathCollection(ISongCollection parent, IEnumerable<Song> shuffleSongs) : base(parent)
        {
            parent.Changed += Parent_CollectionChanged;

            Change(null, shuffleSongs);
        }

        private void Parent_CollectionChanged(object sender, SongCollectionChangedEventArgs e)
        {
            Song[] ordered = GetOrdered(Parent).ToArray();

            Change(e.GetRemoved(), e.GetAdded().Select(s => new ChangeCollectionItem<Song>(Array.IndexOf(ordered, s), s)));
        }

        protected override ShuffleType GetShuffleType()
        {
            return ShuffleType.Path;
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
            return songs.OrderBy(s => s.Path);
        }

        protected override IShuffleCollection GetNewThis(IEnumerable<Song> songs)
        {
            return new ShufflePathCollection(Parent, songs);
        }

        public override void Dispose()
        {
            if (Parent != null) Parent.Changed -= Parent_CollectionChanged;
        }
    }
}
