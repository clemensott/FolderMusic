using MusicPlayer.Data.Shuffle;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data
{
    //public class SongsChangedEventArgs : ShuffleChangedEventArgs
    //{
    //    public ChangedSong[] AddSongs { get; private set; }

    //    public ChangedSong[] RemoveSongs { get; private set; }

    //    internal SongsChangedEventArgs(ChangedSong[] addSongs, ChangedSong[] removeSongs,
    //        ShuffleType oldShuffleType, ShuffleType newShuffleType, List<int> oldShuffleList, 
    //        List<int> newShuffleList, Song oldCurrentSong, Song newCurrentSong) :
    //        base(oldShuffleType, newShuffleType, oldShuffleList, newShuffleList, oldCurrentSong, newCurrentSong)
    //    {
    //        AddSongs = addSongs;
    //        RemoveSongs = removeSongs;
    //    }

    //    internal SongsChangedEventArgs(IList<Song> oldSongs, IList<Song> newSongs, ShuffleType oldShuffleType, ShuffleType newShuffleType,
    //        List<int> oldShuffleList, List<int> newShuffleList, Song oldCurrentSong, Song newCurrentSong) :
    //        base(oldShuffleType, newShuffleType, oldShuffleList, newShuffleList, oldCurrentSong, newCurrentSong)
    //    {
    //        AddSongs = newSongs.Select((s, i) => new ChangedSong(i, s)).Where(c => !oldSongs.Contains(c.Song)).ToArray();
    //        RemoveSongs = oldSongs.Select((s, i) => new ChangedSong(i, s)).Where(c => !newSongs.Contains(c.Song)).ToArray();
    //    }
    //}
}
