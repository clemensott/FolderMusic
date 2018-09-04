using MusicPlayer.Data.NonLoaded;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MusicPlayer.Data
{
    class PlaylistCollection : IPlaylistCollection
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PlaylistCollectionChangedEventHandler Changed;

        private ObservableCollection<IPlaylist> collection;

        public int Count { get { return collection.Count; } }

        public ILibrary Parent { get; private set; }

        public PlaylistCollection(ILibrary parent)
        {
            Parent = parent;

            collection = new ObservableCollection<IPlaylist>();
            collection.CollectionChanged += This_CollectionChanged;
        }

        public PlaylistCollection(ILibrary parent, IEnumerable<IPlaylist> playlists)
        {
            Parent = parent;

            collection = new ObservableCollection<IPlaylist>(playlists);
            collection.CollectionChanged += This_CollectionChanged;
        }

        public PlaylistCollection(ILibrary parent, CurrentPlaySong currentPlaySong)
        {
            Parent = parent;

            collection = new ObservableCollection<IPlaylist>();
            collection.Add(new NonLoadedPlaylist(this, currentPlaySong));
            collection.CollectionChanged += This_CollectionChanged;
        }

        public PlaylistCollection(ILibrary parent, XmlReader reader)
        {
            Parent = parent;
            ReadXml(reader);
        }

        public PlaylistCollection(ILibrary parent, string xmlText)
            : this(parent, XmlConverter.GetReader(xmlText))
        {
        }

        private void This_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        public int IndexOf(IPlaylist playlist)
        {
            return collection.IndexOf(playlist);
        }

        public void Add(IPlaylist playlist)
        {
            Change(Enumerable.Range(0, 1).Select(i => playlist), Enumerable.Empty<IPlaylist>());
        }

        public void Remove(IPlaylist playlist)
        {
            Change(Enumerable.Empty<IPlaylist>(), Enumerable.Range(0, 1).Select(i => playlist));
        }

        public void Change(IEnumerable<IPlaylist> adds, IEnumerable<IPlaylist> removes)
        {
            IPlaylist oldCurrentPlaylist, newCurrentPlaylist;
            newCurrentPlaylist = oldCurrentPlaylist = Parent.CurrentPlaylist;
            int currentPlaylistIndex = collection.IndexOf(oldCurrentPlaylist);

            IPlaylist[] addArray = adds.ToArray();
            IPlaylist[] removeArray = removes.ToArray();

            ChangedPlaylist[] removed = GetRemovedChangedPlaylists(removeArray).ToArray();
            ChangedPlaylist[] added = GetAddedChangedPlaylists(addArray).ToArray();

            if (removed.Length == 0 && added.Length == 0) return;

            if (!collection.Contains(oldCurrentPlaylist))
            {
                if (currentPlaylistIndex < 0) currentPlaylistIndex = 0;
                if (currentPlaylistIndex >= collection.Count) currentPlaylistIndex = collection.Count;
                Parent.CurrentPlaylist = newCurrentPlaylist = collection.ElementAtOrDefault(currentPlaylistIndex);
            }

            var args = new PlaylistsChangedEventArgs(added, removed, oldCurrentPlaylist, newCurrentPlaylist);
            Changed?.Invoke(this, args);
        }

        private IEnumerable<ChangedPlaylist> GetAddedChangedPlaylists(IEnumerable<IPlaylist> adds)
        {
            foreach (IPlaylist addPlaylist in adds?.ToArray() ?? Enumerable.Empty<IPlaylist>())
            {
                if (collection.Contains(addPlaylist)) continue;

                collection.Add(addPlaylist);
                yield return new ChangedPlaylist(collection.IndexOf(addPlaylist), addPlaylist);
            }
        }

        private IEnumerable<ChangedPlaylist> GetRemovedChangedPlaylists(IEnumerable<IPlaylist> removes)
        {
            foreach (IPlaylist removePlaylist in removes?.ToArray() ?? Enumerable.Empty<IPlaylist>())
            {
                int index = collection.IndexOf(removePlaylist);

                if (index == -1) continue;

                collection.Remove(removePlaylist);
                yield return new ChangedPlaylist(index, removePlaylist);
            }
        }

        public void Reset(IEnumerable<IPlaylist> newPlaylists)
        {
            collection.Clear();


            foreach (IPlaylist playlist in newPlaylists)
            {
                collection.Add(playlist);
            }
        }

        public IEnumerator<IPlaylist> GetEnumerator()
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
            collection = new ObservableCollection<IPlaylist>();

            reader.ReadStartElement();

            while (reader.NodeType == XmlNodeType.Element)
            {
                try
                {
                    collection.Add(new Playlist(this, reader));
                }
                catch (Exception e)
                {
                    MobileDebug.Manager.WriteEvent("XmlReadPlaylistCollectionFail", e, collection.Count, "Node: " + reader.NodeType);
                }

                reader.Read();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var playlist in this)
            {
                writer.WriteStartElement("Playlist");
                try
                {
                    playlist.WriteXml(writer);
                }
                catch (Exception e)
                {
                    MobileDebug.Manager.WriteEvent("XmlWritePlaylistCollectionFail", e);
                }
                writer.WriteEndElement();
            }
        }
    }
}
