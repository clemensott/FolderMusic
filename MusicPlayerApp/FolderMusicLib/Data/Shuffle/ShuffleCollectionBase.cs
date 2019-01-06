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
            Song[] oldShuffle = this.ToArray();

            Song[] removeArray = removes?.ToArray() ?? new Song[0];
            Song[] addArray = adds?.ToArray() ?? new Song[0];

            List<ChangeCollectionItem<Song>> removeChanges = new List<ChangeCollectionItem<Song>>();
            List<ChangeCollectionItem<Song>> addChanges = new List<ChangeCollectionItem<Song>>();

            foreach (Song song in removeArray)
            {
                int index = list.IndexOf(song);

                if (index == -1) continue;

                list.RemoveAt(index);
                removeChanges.Add(new ChangeCollectionItem<Song>(index, song));
            }

            foreach (Song song in addArray)
            {
                int index = list.IndexOf(song);

                if (index == -1) continue;

                list.RemoveAt(index);
                removeChanges.Add(new ChangeCollectionItem<Song>(index, song));
            }

            foreach (Song song in addArray)
            {
                addChanges.Add(new ChangeCollectionItem<Song>(Count, song));
                list.Add(song);
            }

            Change(removeChanges.ToArray(), addChanges.ToArray(), oldShuffle);
        }

        public void Change(IEnumerable<Song> removes, IEnumerable<ChangeCollectionItem<Song>> adds)
        {
            Song[] oldShuffle = this.ToArray();

            Song[] removeArray = removes?.ToArray() ?? new Song[0];
            ChangeCollectionItem<Song>[] addArray = adds?.ToArray() ?? new ChangeCollectionItem<Song>[0];

            List<ChangeCollectionItem<Song>> removeChanges = new List<ChangeCollectionItem<Song>>();

            foreach (Song song in removeArray)
            {
                int index = list.IndexOf(song);

                if (index == -1) continue;

                list.RemoveAt(index);
                removeChanges.Add(new ChangeCollectionItem<Song>(index, song));
            }

            foreach (ChangeCollectionItem<Song> change in addArray)
            {
                int index = list.IndexOf(change.Item);

                if (index == -1) continue;

                list.RemoveAt(index);
                removeChanges.Add(new ChangeCollectionItem<Song>(index, change.Item));
            }

            foreach (ChangeCollectionItem<Song> change in addArray.OrderBy(c => c.Index))
            {
                list.Insert(change.Index, change.Item);
            }

            Change(removeChanges.ToArray(), addArray.ToArray(), oldShuffle);
        }

        private void Change(ChangeCollectionItem<Song>[] removeChanges, ChangeCollectionItem<Song>[] addChanges, Song[] oldShuffle)
        {
            if (removeChanges.Length == 0 && addChanges.Length == 0) return;

            var args = new ShuffleCollectionChangedEventArgs(addChanges, removeChanges);
            
            Changed?.Invoke(this, args);

            if (Parent?.Parent == null) return;

            Song currentSong = Parent.Parent.CurrentSong;

            if (this.Contains(currentSong)) UpdateCurrentSong(oldShuffle);
        }

        protected abstract void UpdateCurrentSong(Song[] oldShuffle);

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

        public abstract void Dispose();
    }
}
