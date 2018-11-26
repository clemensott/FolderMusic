using System;
using System.Collections;
using System.Collections.Generic;
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
            IPlaylist oldCurrentPlaylist, newCurrentPlaylist;
            newCurrentPlaylist = oldCurrentPlaylist = Parent?.CurrentPlaylist;
            int currentPlaylistIndex = list.IndexOf(oldCurrentPlaylist);

            IPlaylist[] removeArray = removes?.ToArray() ?? new IPlaylist[0];
            IPlaylist[] addArray = adds?.ToArray() ?? new IPlaylist[0];

            List<ChangeCollectionItem<IPlaylist>> removed = ChangeCollectionItem<IPlaylist>.GetRemovedChanged(removeArray, this);
            List<ChangeCollectionItem<IPlaylist>> added = new List<ChangeCollectionItem<IPlaylist>>();
            IEnumerable<IPlaylist> newList = list.Except(removed.Select(c => c.Item)).Concat(added.Select(c => c.Item));

            foreach (IPlaylist playlist in addArray.OrderBy(p => p.AbsolutePath))
            {
                int index = WouldIndexOf(newList.Select(p => p.AbsolutePath).OrderBy(p => p), playlist.AbsolutePath);
                ChangeCollectionItem<IPlaylist> addChange = new ChangeCollectionItem<IPlaylist>(index, playlist);

                added.Add(addChange);
            }

            if (removed.Count == 0 && added.Count == 0) return;

            if (oldCurrentPlaylist == null) newCurrentPlaylist = newList.FirstOrDefault();
            else if (Parent?.Playlists == this && !newList.Contains(oldCurrentPlaylist))
            {
                if (currentPlaylistIndex < 0) currentPlaylistIndex = 0;
                if (currentPlaylistIndex >= newList.Count()) currentPlaylistIndex = newList.Count() - 1;

                newCurrentPlaylist = newList.ElementAtOrDefault(currentPlaylistIndex);
            }

            foreach (ChangeCollectionItem<IPlaylist> change in removed) list.Remove(change.Item);
            foreach (ChangeCollectionItem<IPlaylist> change in added)
            {
                change.Item.Parent = this;
                list.Insert(change.Index, change.Item);
            }

            var args = new PlaylistCollectionChangedEventArgs(added.ToArray(), removed.ToArray());
            Changed?.Invoke(this, args);

            if (Parent != null) Parent.CurrentPlaylist = newCurrentPlaylist;
        }

        private static int WouldIndexOf(IEnumerable<string> paths, string path)
        {
            List<string> list = paths.ToList();
            if (!list.Contains(path)) list.Add(path);

            //MobileDebug.Service.WriteEvent("WouldIndexOf2", path, list.OrderBy(p => p).IndexOf(path), "Ordered:", list.OrderBy(p => p));

            return list.OrderBy(p => p).IndexOf(path);
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
            foreach (var playlist in this)
            {
                writer.WriteStartElement("Playlist");
                playlist.WriteXml(writer);
                writer.WriteEndElement();
            }
        }
    }
}
