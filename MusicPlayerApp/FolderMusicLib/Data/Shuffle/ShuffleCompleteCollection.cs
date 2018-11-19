using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data.Shuffle
{
    class ShuffleCompleteCollection : ShuffleCollectionBase
    {
        private const int shuffleCompleteListNextCount = 5, shuffleCompleteListPreviousCount = 3;

        private static Random random = new Random();

        public ShuffleCompleteCollection(ISongCollection songs, Song currentSong) : base(songs)
        {
            Change(null, GetStart(songs, currentSong));
        }

        public ShuffleCompleteCollection(ISongCollection songs) : base(songs)
        {
            Parent.Changed += Parent_CollectionChanged;
            Parent.Parent.CurrentSongChanged += Playlist_CurrentSongChanged;
        }

        public ShuffleCompleteCollection(ISongCollection songs, IEnumerable<Song> shuffleSongs) : this(songs)
        {
            Change(null, shuffleSongs.Select((s, i) => new ChangeCollectionItem<Song>(i, s)));
        }

        protected override ShuffleType GetShuffleType()
        {
            return ShuffleType.Complete;
        }

        protected static IEnumerable<ChangeCollectionItem<Song>> GetStart(ISongCollection songs, Song currentSong)
        {
            List<Song> remaining = new List<Song>(songs);
            int shuffleCount = GetCount(songs.Count);
            int currentSongIndex = GetCurrentSongIndex(songs.Count);

            for (int i = 0; i < shuffleCount; i++)
            {
                Song addSong = i == currentSongIndex && currentSong != null ?
                    currentSong : remaining[random.Next(remaining.Count)];

                yield return new ChangeCollectionItem<Song>(i, addSong);

                remaining.Remove(addSong);
            }
        }

        private void Parent_CollectionChanged(object sender, SongCollectionChangedEventArgs e)
        {
            Song currentSong = Parent.Parent.CurrentSong;
            int shuffleIndex = GetCurrentSongIndex(Parent.Count);
            int shuffleCount = GetCount(Parent.Count);
            int currentSongIndex = IndexOf(currentSong);

            List<Song> removes = new List<Song>();
            List<ChangeCollectionItem<Song>> adds = new List<ChangeCollectionItem<Song>>();

            foreach (Song remove in e.GetRemoved())
            {
                int index = IndexOf(remove);

                if (index == -1) continue;

                Song add = GetRandomSong(Parent, null, adds.Select(c => c.Item));
                ChangeCollectionItem<Song> addChange = new ChangeCollectionItem<Song>(index, add);

                removes.Add(remove);
                adds.Add(addChange);
            }

            for (int i = currentSongIndex - 1; i >= shuffleIndex; i++)
            {
                Song remove = this.ElementAt(i);

                if (!removes.Contains(remove)) removes.Add(remove);
                else adds.Remove(adds.FirstOrDefault(c => c.Index == i));
            }

            for (int i = currentSongIndex; i < shuffleIndex; i++)
            {
                Song add = GetRandomSong(Parent, removes, adds.Select(c => c.Item));
                ChangeCollectionItem<Song> addChange = new ChangeCollectionItem<Song>(i, add);
            }

            while (Parent.Count - removes.Count + adds.Count > shuffleCount)
            {
                if (!adds.Remove(adds.FirstOrDefault(c => c.Index > shuffleIndex)))
                {
                    removes.Add(this.Except(removes).LastOrDefault());
                }
            }

            while (Parent.Count - removes.Count + adds.Count < shuffleCount)
            {
                int index = Parent.Count - removes.Count + adds.Count;
                Song add = GetRandomSong(Parent, removes, adds.Select(c => c.Item));
                ChangeCollectionItem<Song> addChange = new ChangeCollectionItem<Song>(index, add);

                adds.Add(addChange);
            }
        }

        private void Playlist_CurrentSongChanged(object sender, CurrentSongChangedEventArgs args)
        {
            Song currentSong = args.NewCurrentSong;
            int shuffleIndex = GetCurrentSongIndex(Parent.Count);
            int currentSongIndex = IndexOf(currentSong);

            if (currentSongIndex == -1)
            {
                Song[] removes = this.Take(Count - shuffleIndex).ToArray();
                List<Song> adds = GetRandomSongs(Parent, removes, Count - shuffleIndex);

                if (!adds.Remove(currentSong)) adds.RemoveAt(0);
                adds.Insert(0, currentSong);

                Change(removes, adds);
            }
            else if (currentSongIndex > shuffleIndex)
            {
                Song[] removes = this.Take(currentSongIndex - shuffleIndex).ToArray();
                List<Song> adds = GetRandomSongs(Parent, removes, currentSongIndex - shuffleIndex);

                Change(removes, adds);
            }
            else
            {
                Song[] removes = this.Skip(Count - shuffleIndex + currentSongIndex).ToArray();
                List<ChangeCollectionItem<Song>> adds = GetRandomSongs(Parent, removes, shuffleIndex - currentSongIndex).
                    Select((c, i) => new ChangeCollectionItem<Song>(i, c)).ToList();

                Change(removes, adds);
            }
        }

        private List<Song> GetRandomSongs(IEnumerable<Song> songs, IEnumerable<Song> removes, int count)
        {
            List<Song> adds = new List<Song>();

            for (int i = 0; i < Count; i++)
            {
                adds.Add(GetRandomSong(songs, removes, adds));
            }

            return adds;
        }

        private Song GetRandomSong(IEnumerable<Song> songs, IEnumerable<Song> removes, IEnumerable<Song> adds)
        {
            if (songs == null) songs = Enumerable.Empty<Song>();
            if (removes == null) removes = Enumerable.Empty<Song>();
            if (adds == null) adds = Enumerable.Empty<Song>();

            IEnumerable<Song> remainingSongs = songs.Except(this.Except(removes)).Except(adds);

            return remainingSongs.ElementAt(random.Next(remainingSongs.Count()));
        }

        private static int GetCurrentSongIndex(int songsCount)
        {
            double divisor = (shuffleCompleteListNextCount + shuffleCompleteListPreviousCount) *
                shuffleCompleteListPreviousCount;

            return (int)((GetCount(songsCount) - 1) / divisor);
        }

        private static int GetCount(int songsCount)
        {
            int count = shuffleCompleteListNextCount + shuffleCompleteListPreviousCount + 1;

            return songsCount > count ? count : songsCount;
        }

        protected override IShuffleCollection GetNewThis(IEnumerable<Song> songs)
        {
            return new ShuffleCompleteCollection(Parent, songs);
        }
    }
}
