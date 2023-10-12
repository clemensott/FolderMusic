using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;

namespace MusicPlayer.Models.Foreground.Shuffle
{
    public interface IShuffleCollection : IEnumerable<Song>, IXmlSerializable, IDisposable
    {
        int Count { get; }
        ShuffleType Type { get; }

        event EventHandler<ShuffleCollectionChangedEventArgs> Changed;

        int IndexOf(Song song);
    }
}
