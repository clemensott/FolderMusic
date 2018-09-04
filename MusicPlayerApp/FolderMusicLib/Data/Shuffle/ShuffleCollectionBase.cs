using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MusicPlayer.Data.Shuffle
{
    public enum ShuffleType { Off, OneTime, Complete }

    abstract class ShuffleCollectionBase : IShuffleCollection
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event ShuffleCollectionChangedEventHandler Changed;

        private ObservableCollection<Song> collection;

        public IPlaylist Parent { get; set; }

        public ISongCollection Songs { get; private set; }

        public ShuffleType Type { get { return GetShuffleType(); } }

        public int Count { get { return collection.Count; } }

        public ShuffleCollectionBase(IPlaylist parent, ISongCollection songs, IEnumerable<Song> shuffleSongs)
        {
            Parent = parent;
            Songs = songs;
            songs.CollectionChanged += Songs_CollectionChanged;

            collection = new ObservableCollection<Song>(shuffleSongs);
            collection.CollectionChanged += This_CollectionChanged;
        }

        public ShuffleCollectionBase(IPlaylist parent, ISongCollection songs, XmlReader reader)
        {
            Parent = parent;
            Songs = songs;
            songs.CollectionChanged += Songs_CollectionChanged;

            ReadXml(reader);
        }

        protected ObservableCollection<Song> GetCollection()
        {
            return collection;
        }

        private void This_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        protected abstract ShuffleType GetShuffleType();

        public int IndexOf(Song song)
        {
            return collection.IndexOf(song);
        }

        public void Reset(IEnumerable<Song> newShuffleSongs)
        {
            collection.Clear();

            foreach (Song song in newShuffleSongs) collection.Add(song);

            if (newShuffleSongs.Any()) RaiseChange();
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
            return collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            collection = new ObservableCollection<Song>();

            try
            {
                if (reader.Name != "string") reader.ReadStartElement();

                while (reader.NodeType == XmlNodeType.Element)
                {
                    try
                    {
                        string path = reader.ReadElementContentAsString();
                        Song song = Songs.FirstOrDefault(s => s.Path == path);

                        if (!(song?.IsEmpty ?? true)) collection.Add(song);
                    }
                    catch (Exception e)
                    {
                        MobileDebug.Manager.WriteEvent("ShuffleCollectionReadXmlFail1", e, reader.NodeType, reader.Name);
                    }
                }
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("ShuffleCollectionBaseReadXmlFail2", e);
            }

            collection.CollectionChanged += This_CollectionChanged;
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
