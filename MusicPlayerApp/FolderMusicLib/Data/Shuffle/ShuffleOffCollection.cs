using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MusicPlayer.Data.Shuffle
{
    class ShuffleOffCollection : ShuffleCollectionBase
    {
        public ShuffleOffCollection(IPlaylist parent, ISongCollection songs) : base(parent, songs, GetStart(songs))
        {
            foreach (Song song in this)
            {
                Subscribe(song);
            }
        }

        public ShuffleOffCollection(IPlaylist parent, ISongCollection songs, XmlReader reader)
            : base(parent, songs, reader)
        {

        }

        public ShuffleOffCollection(IPlaylist parent, ISongCollection songs, string xmlText)
            : this(parent, songs, XmlConverter.GetReader(xmlText))
        {
        }

        private static IEnumerable<Song> GetStart(ISongCollection songs)
        {
            return songs.OrderBy(s => s.Title).ThenBy(s => s.Artist);
        }

        private void Subscribe(Song song)
        {

        }

        private void Unsubscribe(Song song)
        {

        }

        private void Song_Changed(Song sender, EventArgs args)
        {
            UpdateOrder();
        }

        protected override ShuffleType GetShuffleType()
        {
            return ShuffleType.Off;
        }

        protected override void UpdateCollection(SongCollectionChangedEventArgs args)
        {
            bool changed = false;
            var collection = GetCollection();

            foreach (Song addSong in args.GetAdded())
            {
                changed = true;

                collection.Add(addSong);
                Subscribe(addSong);
            }

            foreach (Song removeSong in args.GetRemoved())
            {
                changed = true;

                collection.Remove(removeSong);
                Unsubscribe(removeSong);
            }

            if (UpdateOrder()) changed = true;
            if (changed) RaiseChange();
        }

        private bool UpdateOrder()
        {
            bool changed = false;
            var collection = GetCollection();
            Song[] orderedArray = collection.OrderBy(s => s.Title).ThenBy(s => s.Artist).ToArray();

            for (int i = 0; i < orderedArray.Length; i++)
            {
                int currentIndex = collection.IndexOf(orderedArray[i]);

                if (i == currentIndex) continue;

                changed = true;
                collection.Move(currentIndex, i);
            }

            return changed;
        }
    }
}
