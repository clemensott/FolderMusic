﻿using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml.Serialization;

namespace MusicPlayer.Data.Shuffle
{
    public delegate void ShuffleCollectionChangedEventHandler(IShuffleCollection sender);

    public interface IShuffleCollection : IEnumerable<Song>, INotifyCollectionChanged, IXmlSerializable
    {
        int Count { get; }
        IPlaylist Parent { get; }
        ISongCollection Songs { get; }
        ShuffleType Type { get; }

        event ShuffleCollectionChangedEventHandler Changed;

        int IndexOf(Song song);
        void Reset(IEnumerable<Song> newShuffleSongs);
    }
}