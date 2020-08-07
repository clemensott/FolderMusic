using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models.Shuffle
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

            foreach (Song addSong in e.GetAdded())
            {
                int index = rnd.Next(Count - e.RemovedSongs.Length + adds.Count);

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


        protected override IShuffleCollection GetNewThis(IEnumerable<Song> songs)
        {
            return new ShuffleOneTimeCollection(parent, songs);
        }
    }
}
