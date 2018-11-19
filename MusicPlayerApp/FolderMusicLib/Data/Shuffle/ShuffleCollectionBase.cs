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
        public event EventHandler<ShuffleCollectionChangedEventArgs> Changed;

        private ISongCollection songs;
        private List<Song> list;

        public ISongCollection Parent { get; private set; }

        public ShuffleType Type { get { return GetShuffleType(); } }

        public int Count { get { return list.Count; } }

        public ShuffleCollectionBase(ISongCollection parent)
        {
            list = new List<Song>();

            Parent = parent;
        }

        protected abstract ShuffleType GetShuffleType();

        public int IndexOf(Song song)
        {
            return list.IndexOf(song);
        }

        protected void Change(IEnumerable<Song> removes, IEnumerable<Song> adds)
        {
            ChangeCollectionItem<Song>[] removeChanges = ChangeCollectionItem<Song>.GetRemovedChanged(removes, list).ToArray();
            ChangeCollectionItem<Song>[] addChanges = ChangeCollectionItem<Song>.GetAddedChanged(adds, list.Except(removes)).ToArray();

            Change(removeChanges, addChanges);
        }

        public void Change(IEnumerable<Song> removes, IEnumerable<ChangeCollectionItem<Song>> adds)
        {
            ChangeCollectionItem<Song>[] removeChanges = ChangeCollectionItem<Song>.GetRemovedChanged(removes, list).ToArray();
            ChangeCollectionItem<Song>[] addChanges = (adds ?? Enumerable.Empty<ChangeCollectionItem<Song>>()).ToArray();

            Change(removeChanges, addChanges);
        }

        private void Change(ChangeCollectionItem<Song>[] removeChanges, ChangeCollectionItem<Song>[] addChanges)
        {
            if (removeChanges.Length == 0 && addChanges.Length == 0) return;

            foreach (ChangeCollectionItem<Song> change in removeChanges) list.Remove(change.Item);
            foreach (ChangeCollectionItem<Song> change in addChanges) list.Insert(change.Index, change.Item);

            var args = new ShuffleCollectionChangedEventArgs(addChanges, removeChanges);
            Changed?.Invoke(this, args);
        }

        public IShuffleCollection Repalce(IEnumerable<Song> songsToRepalce)
        {
            Song[] array = songs.ToArray();
            IEnumerable<Song> newSongs = this.Select(s1 => array.FirstOrDefault(s2 => s2.Path == s1.Path) ?? s1);

            return GetNewThis(newSongs);
        }

        protected abstract IShuffleCollection GetNewThis(IEnumerable<Song> songs);

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
                    Song song = Parent.FirstOrDefault(s => s.Path == path);

                    if (!(song?.IsEmpty ?? true)) list.Add(song);
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("ShuffleCollectionReadXmlFail1", e, reader.NodeType, reader.Name);
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
