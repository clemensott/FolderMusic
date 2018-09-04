using System;
using System.Collections.Generic;
using System.Linq;

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

        public ShuffleCompleteCollection(IPlaylist parent, ISongCollection songs, string xmlText)
            : base(parent, songs, xmlText)
        {
            parent.CurrentSongChanged += Parent_CurrentSongChanged;
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
            int shuffleCount = GetCount(Songs.Count);
            int shuffleIndex = GetCurrentSongIndex(Songs.Count);

            if (!list.Contains(currentSong)) list.Add(currentSong);

            while (list.IndexOf(currentSong) > shuffleIndex)
            {
                list.RemoveAt(0);
                list.Add(Songs.Except(list).ElementAt(random.Next(Songs.Count - list.Count)));
            }

            while (list.IndexOf(currentSong) < shuffleIndex)
            {
                list.RemoveAt(list.Count - 1);
                list.Insert(0, Songs.Except(list).ElementAt(random.Next(Songs.Count - list.Count)));
            }
        }

        protected override void UpdateCollection(SongCollectionChangedEventArgs args)
        {
            bool changed = false;
            Song currentSong = Parent.CurrentSong;
            int shuffleCount = GetCount(Songs.Count);
            int shuffleIndex = GetCurrentSongIndex(Songs.Count);

            foreach (Song removeSong in args.GetRemoved())
            {
                if (list.Remove(removeSong)) changed = true;
            }

            while (list.IndexOf(currentSong) > shuffleIndex)
            {
                changed = true;

                list.RemoveAt(0);
                list.Add(Songs.Except(list).ElementAt(random.Next(Songs.Count - list.Count)));
            }

            while (list.IndexOf(currentSong) < shuffleIndex)
            {
                changed = true;

                list.RemoveAt(list.Count - 1);
                list.Insert(0, Songs.Except(list).ElementAt(random.Next(Songs.Count - list.Count)));
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
