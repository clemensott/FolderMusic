using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models
{
    class PlaylistCollection : IPlaylistCollection
    {
        private List<IPlaylist> list;

        public event EventHandler<PlaylistCollectionChangedEventArgs> Changed;

        public int Count => list.Count;
        
        public PlaylistCollection()
        {
            list = new List<IPlaylist>();
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
                playlist.SongsChanged += Playlist_SongsChanged;
            }

            foreach (IPlaylist playlist in addArray.OrderBy(p => p.AbsolutePath))
            {
                if (this.Contains(playlist) || playlist.Songs == null || playlist.Songs.Count == 0) continue;

                int index = WouldIndexOf(this.Select(p => p.AbsolutePath), playlist.AbsolutePath);

                addChanges.Add(new ChangeCollectionItem<IPlaylist>(index, playlist));
                list.Insert(index, playlist);
                playlist.SongsChanged -= Playlist_SongsChanged;
            }

            if (removeChanges.Count == 0 && addChanges.Count == 0) return;

            PlaylistCollectionChangedEventArgs args =
                new PlaylistCollectionChangedEventArgs(addChanges.ToArray(), removeChanges.ToArray());
            Changed?.Invoke(this, args);
            OnPropertyChanged(nameof(Count));
        }

        private void Playlist_SongsChanged(object sender, SongsChangedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)sender;
            if (playlist.Songs.Count == 0) Remove(playlist);
        }

        private static int WouldIndexOf(IEnumerable<string> paths, string path)
        {
            return paths.Concat(Utils.RepeatOnce(path)).OrderBy(p => p).IndexOf(path);
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
