using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml.Serialization;

namespace MusicPlayer.Data
{
    public delegate void PlaylistCollectionChangedEventHandler(IPlaylistCollection sender, PlaylistsChangedEventArgs args);

    public interface IPlaylistCollection : IEnumerable<IPlaylist>, INotifyCollectionChanged, IXmlSerializable
    {
        int Count { get; }
        ILibrary Parent { get; }

        event PlaylistCollectionChangedEventHandler Changed;

        void Add(IPlaylist playlist);
        void Remove(IPlaylist playlist);
        void Change(IEnumerable<IPlaylist> adds, IEnumerable<IPlaylist> removes);
        int IndexOf(IPlaylist playlist);
        void Reset(IEnumerable<IPlaylist> newPlaylists);
    }
}