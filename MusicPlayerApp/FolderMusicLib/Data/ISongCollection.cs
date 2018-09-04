using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MusicPlayer.Data
{
    public delegate void SongCollectionChangedEventHandler(ISongCollection sender, SongCollectionChangedEventArgs args);

    public interface ISongCollection : IEnumerable<Song>, IXmlSerializable
    {
        int Count { get; }
        IPlaylist Parent { get; }

        event SongCollectionChangedEventHandler CollectionChanged;

        void Add(Song song);
        void Remove(Song song);
        void Change(IEnumerable<Song> adds, IEnumerable<Song> removes);
        int IndexOf(Song song);
        void Reset(IEnumerable<Song> newSongs);
    }
}