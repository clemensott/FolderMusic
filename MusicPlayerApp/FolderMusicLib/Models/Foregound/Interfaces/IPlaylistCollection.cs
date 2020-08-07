using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using MusicPlayer.Models.EventArgs;

namespace MusicPlayer.Models.Interfaces
{
    public interface IPlaylistCollection : IEnumerable<IPlaylist>, INotifyPropertyChanged, IXmlSerializable
    {
        int Count { get; }

        event EventHandler<PlaylistCollectionChangedEventArgs> Changed;

        void Add(IPlaylist playlist);
        void Remove(IPlaylist playlist);
        void Change(IEnumerable<IPlaylist> removes, IEnumerable<IPlaylist> adds);
        int IndexOf(IPlaylist playlist);
    }
}