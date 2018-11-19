using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data.Shuffle
{
    public class ShuffleCollectionChangedEventArgs : EventArgs
    {
        public ChangeCollectionItem<Song>[] AddedSongs { get; private set; }

        public ChangeCollectionItem<Song>[] RemovedSongs { get; private set; }

        public ShuffleCollectionChangedEventArgs(ChangeCollectionItem<Song>[] addSongs, ChangeCollectionItem<Song>[] removeSongs)
        {
            AddedSongs = addSongs ?? new ChangeCollectionItem<Song>[0];
            RemovedSongs = removeSongs ?? new ChangeCollectionItem<Song>[0];
        }

        public IEnumerable<Song> GetAdded()
        {
            return AddedSongs.Select(p => p.Item);
        }

        public IEnumerable<Song> GetRemoved()
        {
            return RemovedSongs.Select(p => p.Item);
        }
    }
}