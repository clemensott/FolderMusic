using System;
using System.Collections.Generic;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Foreground.Interfaces;
using System.Linq;

namespace MusicPlayer.Models.Foreground.Shuffle
{
    class ShuffleOneTimeCollection : ShuffleCollectionBase
    {
        private static Random rnd = new Random();

        public override ShuffleType Type => ShuffleType.OneTime;

        public ShuffleOneTimeCollection(ISongCollection parent, Song? currentSong = null)
            : this(parent, GetStart(parent, currentSong)) { }

        private ShuffleOneTimeCollection(ISongCollection parent, IEnumerable<Song> songs) : base(parent)
        {
            Change(null, songs);
        }

        protected override void OnParentChanged(object sender, SongCollectionChangedEventArgs e)
        {
            List<ChangeCollectionItem<Song>> adds = new List<ChangeCollectionItem<Song>>();

            foreach (Song addSong in e.GetAdded().Where(a => e.GetRemoved().All(r => a.FullPath != r.FullPath)))
            {
                int index;
                do
                {
                    index = rnd.Next(Count - e.RemovedSongs.Length + adds.Count);
                }
                while (adds.All(a => a.Index != index));

                adds.Add(new ChangeCollectionItem<Song>(index, addSong));
            }

            foreach (Song addSong in e.GetAdded().Where(a => e.GetRemoved().Any(r => a.FullPath == r.FullPath)))
            {
                int index = this.IndexOf(s => s.FullPath == addSong.FullPath);

                foreach (int addIndex in adds.Select(a => a.Index).OrderBy(i => i))
                {
                    if (addIndex > index) break;
                    index++;
                }

                adds.Add(new ChangeCollectionItem<Song>(index, addSong));
            }

            Change(e.GetRemoved(), adds);
        }

        private static IEnumerable<Song> GetStart(ISongCollection songs, Song? currentSong)
        {
            List<Song> remaining = new List<Song>(songs);

            if (currentSong.HasValue && remaining.Remove(currentSong.Value)) yield return currentSong.Value;

            while (remaining.Count > 0)
            {
                int index = rnd.Next(remaining.Count);
                yield return remaining[index];

                remaining.RemoveAt(index);
            }
        }
    }
}
