using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Models.EventArgs
{
    public class SongCollectionChangedEventArgs : System.EventArgs
    {
        public ChangeCollectionItem<Song>[] AddedSongs { get; }

        public ChangeCollectionItem<Song>[] RemovedSongs { get; }

        public SongCollectionChangedEventArgs(ChangeCollectionItem<Song>[] addSongs, ChangeCollectionItem<Song>[] removeSongs)
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
