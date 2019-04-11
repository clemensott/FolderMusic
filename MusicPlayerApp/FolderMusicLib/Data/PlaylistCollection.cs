using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace MusicPlayer.Data
{
    class PlaylistCollection : IPlaylistCollection
    {
        private List<IPlaylist> list;

        public event EventHandler<PlaylistCollectionChangedEventArgs> Changed;

        public int Count { get { return list.Count; } }

        public ILibrary Parent { get; set; }

        public PlaylistCollection()
        {
            list = new List<IPlaylist>();
        }

        public PlaylistCollection(CurrentPlaySong currentPlaySong)
        {
            list = new List<IPlaylist>(Utils.RepeatOnce(new Playlist(currentPlaySong)));
        }

        public int IndexOf(IPlaylist playlist)
        {
            return list.IndexOf(playlist);
        }

        public void Add(IPlaylist playlist)
        {
            Change(null, Utils.RepeatOnce(playlist));
        }

        public void Remove(IPlaylist playlist)
        {
            Change(Utils.RepeatOnce(playlist), null);
        }

        public void Change(IEnumerable<IPlaylist> removes, IEnumerable<IPlaylist> adds)
        {
            IPlaylist[] removeArray = removes?.ToArray() ?? new IPlaylist[0];
            IPlaylist[] addArray = adds?.ToArray() ?? new IPlaylist[0];

            List<ChangeCollectionItem<IPlaylist>> removeChanges = new List<ChangeCollectionItem<IPlaylist>>();
            List<ChangeCollectionItem<IPlaylist>> addChanges = new List<ChangeCollectionItem<IPlaylist>>();

            foreach (IPlaylist playlist in removeArray)
            {
                int index = IndexOf(playlist);

                if (index == -1) continue;

                removeChanges.Add(new ChangeCollectionItem<IPlaylist>(index, playlist));
                list.RemoveAt(index);
            }

            foreach (IPlaylist playlist in addArray.OrderBy(p => p.AbsolutePath))
            {
                if (this.Contains(playlist)) continue;

                int index = WouldIndexOf(this.Select(p => p.AbsolutePath), playlist.AbsolutePath);

                addChanges.Add(new ChangeCollectionItem<IPlaylist>(index, playlist));
                list.Insert(index, playlist);

                playlist.Parent = this;
            }

            if (removeChanges.Count == 0 && addChanges.Count == 0) return;

            PlaylistCollectionChangedEventArgs args = new PlaylistCollectionChangedEventArgs(addChanges.ToArray(), removeChanges.ToArray());
            Changed?.Invoke(this, args);
            OnPropertyChanged(nameof(Count));

            UpdateCurrentPlaylist();
        }

        private static int WouldIndexOf(IEnumerable<string> paths, string path)
        {
            return paths.Concat(Enumerable.Repeat(path, 1)).OrderBy(p => p).IndexOf(path);
        }

        private void UpdateCurrentPlaylist()
        {
            if (Parent == null) return;

            IPlaylist currentPlaylist = Parent.CurrentPlaylist;

            if (currentPlaylist != null)
            {
                if (Count == 0) Parent.CurrentPlaylist = null;
                else
                {
                    int index = WouldIndexOf(this.Select(p => p.AbsolutePath), currentPlaylist.AbsolutePath) % Count;
                    Parent.CurrentPlaylist = this.ElementAtOrDefault(index);
                }
            }
            else Parent.CurrentPlaylist = this.FirstOrDefault();
        }

        public IPlaylistCollection ToSimple()
        {
            IPlaylistCollection collection = new PlaylistCollection();
            collection.Change(null, this.Select(p => p.ToSimple()));

            return collection;
        }

        public IEnumerator<IPlaylist> GetEnumerator()
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
            reader.ReadStartElement();

            list = XmlConverter.DeserializeList<Playlist>(reader).Cast<IPlaylist>().ToList();

            foreach (IPlaylist playlist in list) playlist.Parent = this;
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (IPlaylist playlist in this)
            {
                writer.WriteStartElement("Playlist");
                playlist.WriteXml(writer);
                writer.WriteEndElement();
            }
        }
    }
}
