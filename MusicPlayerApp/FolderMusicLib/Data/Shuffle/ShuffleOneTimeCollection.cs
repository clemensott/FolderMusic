using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MusicPlayer.Data.Shuffle
{
    class ShuffleOneTimeCollection : ShuffleCollectionBase
    {
        private static Random random = new Random();

        public ShuffleOneTimeCollection(IPlaylist parent, ISongCollection songs, Song currentSong)
            : base(parent, songs, GetStart(songs, currentSong))
        {
        }

        public ShuffleOneTimeCollection(IPlaylist parent, ISongCollection songs, XmlReader reader)
            : base(parent, songs, reader)
        {
        }

        public ShuffleOneTimeCollection(IPlaylist parent, ISongCollection songs, string xmlText)
            : this(parent, songs, XmlConverter.GetReader(xmlText))
        {
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
                int index = random.Next(remaining.Count);
                yield return remaining[index];

                remaining.RemoveAt(index);
            }
        }

        protected override void UpdateCollection(SongCollectionChangedEventArgs args)
        {
            bool changed = false;
            var collection = GetCollection();

            foreach (Song addSong in args.GetAdded())
            {
                changed = true;

                collection.Insert(random.Next(collection.Count + 1), addSong);
            }

            foreach (Song removeSong in args.GetRemoved())
            {
                changed = true;

                collection.Remove(removeSong);
            }

            if (changed) RaiseChange();
        }
    }
}
