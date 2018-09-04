using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace MusicPlayer.Data
{
    public class SongCollection : ISongCollection
    {
        public event SongCollectionChangedEventHandler Changed;

        private List<Song> list;

        public int Count { get { return list.Count; } }

        public IPlaylist Parent { get; private set; }

        public SongCollection(IPlaylist parent)
        {
            Parent = parent;
            list = new List<Song>();
        }

        public SongCollection(IPlaylist parent, string xmlText)
        {
            Parent = parent;
            ReadXml(XmlConverter.GetReader(xmlText));
        }

        public void Reset(IEnumerable<Song> newSongs)
        {
            Change(newSongs, this);
            //list = new List<Song>(newSongs);
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
            Song oldCurrentSong, newCurrentSong;
            newCurrentSong = oldCurrentSong = Parent.CurrentSong;
            int currentSongIndex = list.IndexOf(oldCurrentSong);

            Song[] addArray = adds.ToArray();
            Song[] removeArray = removes.ToArray();

            ChangedSong[] removed = GetRemovedChangedSongs(removeArray).ToArray();
            ChangedSong[] added = GetAddedChangedSongs(addArray).ToArray();

            if (removed.Length == 0 && added.Length == 0) return;

            if (!list.Contains(oldCurrentSong))
            {
                if (currentSongIndex < 0) currentSongIndex = 0;
                if (currentSongIndex >= list.Count) currentSongIndex = list.Count;
                Parent.CurrentSong = newCurrentSong = list.ElementAtOrDefault(currentSongIndex);
            }

            var args = new SongCollectionChangedEventArgs(added, removed, oldCurrentSong, newCurrentSong);
            Changed?.Invoke(this, args);
        }

        private IEnumerable<ChangedSong> GetAddedChangedSongs(IEnumerable<Song> adds)
        {
            foreach (Song addSong in adds?.ToArray() ?? Enumerable.Empty<Song>())
            {
                if (list.Contains(addSong)) continue;

                list.Add(addSong);
                yield return new ChangedSong(list.IndexOf(addSong), addSong);
            }
        }

        private IEnumerable<ChangedSong> GetRemovedChangedSongs(IEnumerable<Song> removes)
        {
            foreach (Song removeSong in removes?.ToArray() ?? Enumerable.Empty<Song>())
            {
                int index = list.IndexOf(removeSong);

                if (index == -1) continue;

                list.Remove(removeSong);
                yield return new ChangedSong(index, removeSong);
            }
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
