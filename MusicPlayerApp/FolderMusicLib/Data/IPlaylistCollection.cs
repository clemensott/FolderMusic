using System.Collections.Generic;
using System.Xml.Serialization;

namespace MusicPlayer.Data
{
    public delegate void PlaylistCollectionChangedEventHandler(IPlaylistCollection sender, PlaylistCollectionChangedEventArgs args);

    public interface IPlaylistCollection : IEnumerable<IPlaylist>, IXmlSerializable
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