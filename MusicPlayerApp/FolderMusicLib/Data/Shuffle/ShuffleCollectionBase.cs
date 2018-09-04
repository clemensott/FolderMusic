using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace MusicPlayer.Data.Shuffle
{
    public enum ShuffleType { Off, OneTime, Complete }

    abstract class ShuffleCollectionBase : IShuffleCollection
    {
        public event ShuffleCollectionChangedEventHandler Changed;

        protected List<Song> list;

        public IPlaylist Parent { get; set; }

        public ISongCollection Songs { get; private set; }

        public ShuffleType Type { get { return GetShuffleType(); } }

        public int Count { get { return list.Count; } }

        public ShuffleCollectionBase(IPlaylist parent, ISongCollection songs, IEnumerable<Song> shuffleSongs)
        {
            Parent = parent;
            Songs = songs;
            songs.Changed += Songs_CollectionChanged;

            list = new List<Song>(shuffleSongs);
        }

        public ShuffleCollectionBase(IPlaylist parent, ISongCollection songs, string xmlText)
        {
            Parent = parent;
            Songs = songs;
            songs.Changed += Songs_CollectionChanged;

            ReadXml(XmlConverter.GetReader(xmlText));
        }

        protected abstract ShuffleType GetShuffleType();

        public int IndexOf(Song song)
        {
            return list.IndexOf(song);
        }

        public void Reset(IEnumerable<Song> newShuffleSongs)
        {
            list.Clear();
            list.AddRange(newShuffleSongs);

            RaiseChange();
        }

        private void Songs_CollectionChanged(ISongCollection sender, SongCollectionChangedEventArgs args)
        {
            UpdateCollection(args);
        }

        protected abstract void UpdateCollection(SongCollectionChangedEventArgs args);

        protected void RaiseChange()
        {
            Changed?.Invoke(this);
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
                try
                {
                    string path = reader.ReadElementContentAsString();
                    Song song = Songs.FirstOrDefault(s => s.Path == path);

                    if (!(song?.IsEmpty ?? true)) list.Add(song);
                }
                catch (Exception e)
                {
                    MobileDebug.Manager.WriteEvent("ShuffleCollectionReadXmlFail1", e, reader.NodeType, reader.Name);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (Song song in this)
            {
                writer.WriteElementString("string", song.Path);
            }
        }
    }
}
