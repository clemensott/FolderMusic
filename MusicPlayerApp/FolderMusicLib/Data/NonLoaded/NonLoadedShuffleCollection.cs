using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;

namespace MusicPlayer.Data.NonLoaded
{
    class NonLoadedShuffleCollection : IShuffleCollection
    {
        private List<Song> list;

        public int Count { get { return list.Count; } }

        public IPlaylist Parent { get; private set; }

        public ISongCollection Songs { get; private set; }

        public ShuffleType Type { get; private set; }

        public event ShuffleCollectionChangedEventHandler Changed;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public NonLoadedShuffleCollection(IPlaylist parent, ISongCollection songs, ShuffleType type)
        {
            Parent = parent;
            Songs = songs;
            Type = type;
            list = new List<Song>(songs);
        }

        public int IndexOf(Song song)
        {
            return list.IndexOf(song);
        }

        public void Reset(IEnumerable<Song> newShuffleSongs)
        {
            
        }

        public IEnumerator<Song> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
        }

        public void WriteXml(XmlWriter writer)
        {
        }
    }
}
