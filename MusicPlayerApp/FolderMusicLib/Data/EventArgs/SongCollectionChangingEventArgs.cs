using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Data
{
    public class SongCollectionChangedEventArgs : EventArgs
    {
        public ChangedSong[] AddedSongs { get; private set; }

        public ChangedSong[] RemovedSongs { get; private set; }

        public Song OldCurrentSong { get; private set; }

        public Song NewCurrentSong { get; private set; }

        public SongCollectionChangedEventArgs(ChangedSong[] addSongs,
            ChangedSong[] removeSongs, Song oldCurrentSong, Song newCurrentSong)
        {
            AddedSongs = addSongs ?? new ChangedSong[0];
            RemovedSongs = removeSongs ?? new ChangedSong[0];

            OldCurrentSong = oldCurrentSong;
            NewCurrentSong = newCurrentSong;
        }

        public IEnumerable<Song> GetAdded()
        {
            return AddedSongs.Select(p => p.Song);
        }

        public IEnumerable<Song> GetRemoved()
        {
            return RemovedSongs.Select(p => p.Song);
        }
    }
}
