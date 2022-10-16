using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Foreground.Interfaces;

namespace MusicPlayer.Models.Foreground.Shuffle
{
    abstract class ShuffleCollectionBase : IShuffleCollection
    {
        public event EventHandler<ShuffleCollectionChangedEventArgs> Changed;

        protected readonly ISongCollection parent;
        private List<Song> list;

        public abstract ShuffleType Type { get; }

        public int Count => list.Count;

        public ShuffleCollectionBase(ISongCollection parent)
        {
            this.parent = parent;
            parent.Changed += OnParentChanged;

            list = new List<Song>();
        }

        public int IndexOf(Song song)
        {
            return list.IndexOf(song);
        }

        protected virtual void OnParentChanged(object sender, SongCollectionChangedEventArgs e) { }

        protected void Change(IEnumerable<Song> removes, IEnumerable<Song> adds)
        {
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

            Change(removeChanges.ToArray(), addChanges.ToArray());
        }

        protected void Change(IEnumerable<Song> removes, IEnumerable<ChangeCollectionItem<Song>> adds)
        {
            Song[] oldShuffle = this.ToArray();

            Song[] removeArray = removes?.ToArray() ?? new Song[0];
            ChangeCollectionItem<Song>[] addArray = adds?.ToArray() ?? new ChangeCollectionItem<Song>[0];

            List<ChangeCollectionItem<Song>> removeChanges = new List<ChangeCollectionItem<Song>>();

            foreach (Song song in removeArray)
            {
                int index = list.IndexOf(song);

                if (index != -1) removeChanges.Add(new ChangeCollectionItem<Song>(index, song));
            }

            foreach (ChangeCollectionItem<Song> change in addArray)
            {
                int index = list.IndexOf(change.Item);

                if (index != -1) removeChanges.Add(new ChangeCollectionItem<Song>(index, change.Item));
            }

            foreach (ChangeCollectionItem<Song> change in removeChanges) list.Remove(change.Item);

            foreach (ChangeCollectionItem<Song> change in addArray.OrderBy(c => c.Index))
            {
                list.Insert(change.Index, change.Item);
            }

            Change(removeChanges.OrderBy(c => c.Index).ToArray(), addArray.OrderBy(c => c.Index).ToArray());
        }

        private void Change(ChangeCollectionItem<Song>[] removeChanges, ChangeCollectionItem<Song>[] addChanges)
        {
            if (removeChanges.Length == 0 && addChanges.Length == 0) return;

            ShuffleCollectionChangedEventArgs args = new ShuffleCollectionChangedEventArgs(addChanges, removeChanges);
            Changed?.Invoke(this, args);
            OnPropertyChanged(nameof(Count));
        }

        public IEnumerator<Song> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
                    Song song;
                    string path = reader.ReadElementContentAsString();
                    if (parent.TryGetSong(path, out song)) list.Add(song);
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
                writer.WriteElementString("string", song.FullPath);
            }
        }

        public void Dispose()
        {
            parent.Changed -= OnParentChanged;
        }
    }
}
