using MusicPlayer.Data.Shuffle;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System;

namespace MusicPlayer.Data.Simple
{
    class SimpleSongCollection : ISongCollection
    {
        private const int saveSongsCount = 10;

        private List<Song> list;
        private IShuffleCollection shuffle;

        public event EventHandler<SongCollectionChangedEventArgs> Changed;
        public event EventHandler<ShuffleChangedEventArgs> ShuffleChanged;

        public int Count { get { return list.Count; } }

        public IPlaylist Parent { get; set; }

        public IShuffleCollection Shuffle
        {
            get { return shuffle; }
            set
            {
                if (value == shuffle) return;

                var args = new ShuffleChangedEventArgs(shuffle, value);
                shuffle = value;
                ShuffleChanged?.Invoke(this, args);
            }
        }

        public SimpleSongCollection()
        {
            list = new List<Song>();
        }

        public SimpleSongCollection(IShuffleCollection actualShuffleSongs, Song currentSong)
        {
            list = new List<Song>();

            int currentSongIndex = actualShuffleSongs.IndexOf(currentSong);
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

            SetShuffleType(actualShuffleSongs.Type);
        }

        public SimpleSongCollection(CurrentPlaySong currentPlaySong)
        {
            list = new List<Song>();
            list.Add(new Song(currentPlaySong));
        }

        public int IndexOf(Song song)
        {
            return list.IndexOf(song);
        }

        public void Add(Song song)
        {
            Change(null, Utils.RepeatOnce(song));
        }

        public void Remove(Song song)
        {
            Change(Utils.RepeatOnce(song), null);
        }

        public void Change(IEnumerable<Song> removes, IEnumerable<Song> adds)
        {
        }

        public void SetShuffleType(ShuffleType type)
        {
            Shuffle = new SimpleShuffleCollection(this, type);
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
            ShuffleType shuffleType = (ShuffleType)Enum.Parse(typeof(ShuffleType), reader.GetAttribute("Shuffle"));

            reader.ReadStartElement();
            list = new List<Song>(XmlConverter.DeserializeList<Song>(reader, "Song"));

            foreach (Song song in list) song.Parent = this;

            Shuffle = new SimpleShuffleCollection(this, shuffleType);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Shuffle", Enum.GetName(typeof(ShuffleType), Shuffle.Type));

            foreach (Song song in this)
            {
                writer.WriteStartElement("Song");
                song.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        public ISongCollection ToSimple()
        {
            return new SimpleSongCollection(Shuffle, this.FirstOrDefault());
        }
    }
}
