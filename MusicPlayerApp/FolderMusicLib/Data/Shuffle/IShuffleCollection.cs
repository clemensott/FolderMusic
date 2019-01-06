using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MusicPlayer.Data.Shuffle
{
    public interface IShuffleCollection : IEnumerable<Song>, IXmlSerializable, IDisposable
    {
        int Count { get; }
        ISongCollection Parent { get; }
        ShuffleType Type { get; }

        event EventHandler<ShuffleCollectionChangedEventArgs> Changed;

        void Change(IEnumerable<Song> removes, IEnumerable<ChangeCollectionItem<Song>> adds);
        int IndexOf(Song song);
    }
}