using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Foreground.Interfaces;

namespace MusicPlayer.Models.Foreground.Shuffle
{
    class ShuffleOffCollection : ShuffleCollectionBase
    {
        public override ShuffleType Type => ShuffleType.Off;

        public ShuffleOffCollection(ISongCollection parent) : this(parent, GetOrdered(parent)) { }

        private ShuffleOffCollection(ISongCollection parent, IEnumerable<Song> shuffleSongs) : base(parent)
        {
            Change(null, shuffleSongs);
        }

        protected override void OnParentChanged(object sender, SongCollectionChangedEventArgs e)
        {
            Song[] ordered = GetOrdered(parent).ToArray();

            Change(e.GetRemoved(),
                e.GetAdded().Select(s => new ChangeCollectionItem<Song>(Array.IndexOf(ordered, s), s)));
        }

        private static IEnumerable<Song> GetOrdered(IEnumerable<Song> songs)
        {
            return songs.OrderBy(s => s.Title).ThenBy(s => s.Artist);
        }

        protected override IShuffleCollection GetNewThis(IEnumerable<Song> songs)
        {
            return new ShuffleOffCollection(parent, songs);
        }
    }
}
