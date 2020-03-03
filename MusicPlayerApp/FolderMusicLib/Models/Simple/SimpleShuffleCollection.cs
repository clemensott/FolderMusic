using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Schema;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;
using MusicPlayer.Models.Shuffle;

namespace MusicPlayer.Models.Simple
{
    class SimpleShuffleCollection : IShuffleCollection
    {
        public event EventHandler<ShuffleCollectionChangedEventArgs> Changed;

        public int Count => Parent.Count;

        public ISongCollection Parent { get; private set; }

        public ShuffleType Type { get; private set; }

        public SimpleShuffleCollection(ISongCollection parent, ShuffleType type)
        {
            Parent = parent;
            Type = type;
        }

        public int IndexOf(Song song)
        {
            return Parent.IndexOf(song);
        }

        public void Change(IEnumerable<Song> removes, IEnumerable<ChangeCollectionItem<Song>> adds)
        {
        }

        public IEnumerator<Song> GetEnumerator()
        {
            return Parent.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Parent.GetEnumerator();
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
        }

        public void WriteXml(XmlWriter writer)
        {
        }

        public void Dispose()
        {
        }
    }
}
