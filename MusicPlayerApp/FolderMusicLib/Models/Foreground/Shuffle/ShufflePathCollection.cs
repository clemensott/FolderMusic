using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Foreground.Interfaces;

namespace MusicPlayer.Models.Foreground.Shuffle
{
    class ShufflePathCollection : ShuffleCollectionBase
    {
        public override ShuffleType Type => ShuffleType.Path;

        public ShufflePathCollection(ISongCollection parent) : this(parent, GetOrdered(parent)) { }

        private ShufflePathCollection(ISongCollection parent, IEnumerable<Song> shuffleSongs) : base(parent)
        {
            Change(null, shuffleSongs);
        }

        protected override void OnParentChanged(object sender, SongCollectionChangedEventArgs e)
        {
            Song[] ordered = GetOrdered(parent).ToArray();

            Change(e.GetRemoved(),
                e.GetAdded().Select(s => new ChangeCollectionItem<Song>(Array.IndexOf(ordered, s), s)));
        }

        private static IOrderedEnumerable<Song> GetOrdered(IEnumerable<Song> songs)
        {
            return songs.OrderBy(s => s.FullPath);
        }
    }
}
