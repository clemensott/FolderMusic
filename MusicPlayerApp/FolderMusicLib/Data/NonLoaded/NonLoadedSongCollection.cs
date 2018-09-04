using MusicPlayer.Data.Shuffle;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace MusicPlayer.Data.NonLoaded
{
    class NonLoadedSongCollection : ISongCollection
    {
        private const int saveSongsCount = 10;

        private List<Song> list;

        public int Count { get { return list.Count; } }

        public IPlaylist Parent { get; private set; }

        public event SongCollectionChangedEventHandler Changed;

        public NonLoadedSongCollection(IPlaylist parent)
        {
            Parent = parent;
            list = new List<Song>();
        }

        public NonLoadedSongCollection(IPlaylist parent, IShuffleCollection actualShuffleSongs)
        {
            Parent = parent;
            list = new List<Song>();

            int currentSongIndex = actualShuffleSongs.IndexOf(parent.CurrentSong);
            int startIndex, count;

            if (actualShuffleSongs.Count < saveSongsCount)
            {
                startIndex = 0;
                count = actualShuffleSongs.Count;
            }
            else if (currentSongIndex + saveSongsCount < actualShuffleSongs.Count)
            {
                startIndex = currentSongIndex;
                count = saveSongsCount;
            }
            else
            {
                startIndex = actualShuffleSongs.Count - saveSongsCount;
                count = saveSongsCount;
            }

            foreach (Song song in actualShuffleSongs.Skip(startIndex).Take(count))
            {
                list.Add(song);
            }
        }

        public NonLoadedSongCollection(IPlaylist parent, CurrentPlaySong currentPlaySong)
        {
            Parent = parent;

            list = new List<Song>();
            list.Add(new Song(this, currentPlaySong));
        }

        public NonLoadedSongCollection(IPlaylist parent, string xmlText)
        {
            Parent = parent;
            ReadXml(XmlConverter.GetReader(xmlText));
        }

        public int IndexOf(Song song)
        {
            return list.IndexOf(song);
        }

        public void Add(Song song)
        {
            Change(Enumerable.Range(0, 1).Select(i => song), Enumerable.Empty<Song>());
        }

        public void Remove(Song song)
        {
            Change(Enumerable.Empty<Song>(), Enumerable.Range(0, 1).Select(i => song));
        }

        public void Change(IEnumerable<Song> adds, IEnumerable<Song> removes)
        {
        }

        public void Reset(IEnumerable<Song> newSongs)
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
            list = new List<Song>();
            reader.ReadStartElement();

            while (reader.NodeType == XmlNodeType.Element)
            {
                list.Add(new Song(this, reader.ReadOuterXml()));
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (Song song in this)
            {
                writer.WriteStartElement("Song");
                song.WriteXml(writer);
                writer.WriteEndElement();
            }
        }
    }
}
