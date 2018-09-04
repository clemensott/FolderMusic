using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MusicPlayer.Data.Shuffle
{
    class ShuffleCompleteCollection : ShuffleCollectionBase
    {
        private const int shuffleCompleteListNextCount = 5, shuffleCompleteListPreviousCount = 3;

        private static Random random = new Random();

        public ShuffleCompleteCollection(IPlaylist parent, ISongCollection songs, Song currentSong)
            : base(parent, songs, GetStart(songs, currentSong))
        {
            parent.CurrentSongChanged += Parent_CurrentSongChanged;
        }

        public ShuffleCompleteCollection(IPlaylist parent, ISongCollection songs, XmlReader reader)
            : base(parent, songs, reader)
        {
            parent.CurrentSongChanged += Parent_CurrentSongChanged;
        }

        public ShuffleCompleteCollection(IPlaylist parent, ISongCollection songs, string xmlText)
            : this(parent, songs, XmlConverter.GetReader(xmlText))
        {
        }

        protected override ShuffleType GetShuffleType()
        {
            return ShuffleType.Complete;
        }

        protected static IEnumerable<Song> GetStart(ISongCollection songs, Song currentSong)
        {
            List<Song> remaining = new List<Song>(songs);
            int shuffleCount = GetCount(songs.Count);
            int currentSongIndex = GetCurrentSongIndex(songs.Count);

            for (int i = 0; i < shuffleCount; i++)
            {
                Song addSong = i == currentSongIndex && currentSong != null ?
                    currentSong : remaining[random.Next(remaining.Count)];

                yield return addSong;

                remaining.Remove(addSong);
            }
        }

        private void Parent_CurrentSongChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            Song currentSong = sender.CurrentSong;
            var collection = GetCollection();
            int shuffleCount = GetCount(Songs.Count);
            int shuffleIndex = GetCurrentSongIndex(Songs.Count);

            if (!collection.Contains(currentSong)) collection.Add(currentSong);

            while (collection.IndexOf(currentSong) > shuffleIndex)
            {
                collection.RemoveAt(0);
                collection.Add(Songs.Except(collection).ElementAt(random.Next(Songs.Count - collection.Count)));
            }

            while (collection.IndexOf(currentSong) < shuffleIndex)
            {
                collection.RemoveAt(collection.Count - 1);
                collection.Insert(0, Songs.Except(collection).ElementAt(random.Next(Songs.Count - collection.Count)));
            }
        }

        protected override void UpdateCollection(SongCollectionChangedEventArgs args)
        {
            bool changed = false;
            Song currentSong = Parent.CurrentSong;
            var collection = GetCollection();
            int shuffleCount = GetCount(Songs.Count);
            int shuffleIndex = GetCurrentSongIndex(Songs.Count);

            foreach (Song removeSong in args.GetRemoved())
            {
                if (collection.Remove(removeSong)) changed = true;
            }

            while (collection.IndexOf(currentSong) > shuffleIndex)
            {
                changed = true;

                collection.RemoveAt(0);
                collection.Add(Songs.Except(collection).ElementAt(random.Next(Songs.Count - collection.Count)));
            }

            while (collection.IndexOf(currentSong) < shuffleIndex)
            {
                changed = true;

                collection.RemoveAt(collection.Count - 1);
                collection.Insert(0, Songs.Except(collection).ElementAt(random.Next(Songs.Count - collection.Count)));
            }

            if (changed) RaiseChange();
        }

        private static int GetCurrentSongIndex(int songsCount)
        {
            double indexDouble = (GetCount(songsCount) - 1) /
                Convert.ToDouble(shuffleCompleteListNextCount + shuffleCompleteListPreviousCount) *
                shuffleCompleteListPreviousCount;

            return Convert.ToInt32(indexDouble);
        }

        private static int GetCount(int songsCount)
        {
            int count = shuffleCompleteListNextCount + shuffleCompleteListPreviousCount + 1;

            return songsCount > count ? count : songsCount;
        }
    }
}
