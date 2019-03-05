using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace MusicPlayer.Data
{
    public interface ISongCollection : IEnumerable<Song>, INotifyPropertyChanged, IXmlSerializable
    {
        int Count { get; }
        IPlaylist Parent { get; set; }
        IShuffleCollection Shuffle { get; set; }

        event EventHandler<SongCollectionChangedEventArgs> Changed;
        event EventHandler<ShuffleChangedEventArgs> ShuffleChanged;

        void Add(Song song);
        void Remove(Song song);
        void Change(IEnumerable<Song> removes, IEnumerable<Song> adds);
        int IndexOf(Song song);
        void SetShuffleType(ShuffleType type);
        ISongCollection ToSimple();
    }
}