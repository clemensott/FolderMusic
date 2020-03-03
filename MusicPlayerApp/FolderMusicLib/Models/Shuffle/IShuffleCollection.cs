using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models.Shuffle
{
    public interface IShuffleCollection : IEnumerable<Song>, INotifyPropertyChanged, IXmlSerializable, IDisposable
    {
        int Count { get; }
        ISongCollection Parent { get; }
        ShuffleType Type { get; }

        event EventHandler<ShuffleCollectionChangedEventArgs> Changed;

        void Change(IEnumerable<Song> removes, IEnumerable<ChangeCollectionItem<Song>> adds);
        int IndexOf(Song song);
    }
}