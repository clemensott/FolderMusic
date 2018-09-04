using System;
using System.Collections.Generic;
using System.Linq;

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

        public ShuffleOffCollection(IPlaylist parent, ISongCollection songs, string xmlText)
            : base(parent, songs, xmlText)
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

            foreach (Song addSong in args.GetAdded())
            {
                changed = true;

                list.Add(addSong);
                Subscribe(addSong);
            }

            foreach (Song removeSong in args.GetRemoved())
            {
                changed = true;

                list.Remove(removeSong);
                Unsubscribe(removeSong);
            }

            if (UpdateOrder()) changed = true;
            if (changed) RaiseChange();
        }

        private bool UpdateOrder()
        {
            bool changed = false;
            Song[] orderedArray = list.OrderBy(s => s.Title).ThenBy(s => s.Artist).ToArray();

            for (int i = 0; i < orderedArray.Length; i++)
            {
                int currentIndex = list.IndexOf(orderedArray[i]);

                if (i == currentIndex) continue;

                Song moveSong = orderedArray[i];
                list.RemoveAt(currentIndex);
                list.Insert(i, moveSong);

                changed = true;
            }

            return changed;
        }
    }
}
