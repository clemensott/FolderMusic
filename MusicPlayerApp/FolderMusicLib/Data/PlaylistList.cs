using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data
{
    public class PlaylistList : IList<Playlist>
    {
        protected List<Playlist> playlists;

        public Playlist this[int index]
        {
            get
            {
                return GetThis(index);
            }

            set
            {
                SetThis(index, value);
            }
        }

        public int Count
        {
            get
            {
                return GetCount();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public PlaylistList()
        {
            playlists = new List<Playlist>();
        }

        public PlaylistList(IEnumerable<Playlist> playlists)
        {
            this.playlists = new List<Playlist>(playlists);
        }

        public virtual void Add(Playlist item)
        {
            if (Contains(item)) return;

            playlists.Add(item);

            Playlist currentPlaylist = Library.Current.CurrentPlaylist;
            ChangedPlaylist[] addPlaylists = new ChangedPlaylist[] { new ChangedPlaylist(Count - 1, item) };

            Feedback.Current.RaisePlaylistsPropertyChanged(addPlaylists, new ChangedPlaylist[0], currentPlaylist, currentPlaylist);
        }

        public virtual void Clear()
        {
            Playlist oldCurrentPlaylist =  Library.Current.CurrentPlaylist;

            playlists.Clear();

            Playlist newCurrentPlaylist = Library.Current.CurrentPlaylist;

            Feedback.Current.RaisePlaylistsPropertyChanged(new ChangedPlaylist[0],
                this.Select((p, i) => new ChangedPlaylist(i, p)).ToArray(), oldCurrentPlaylist, newCurrentPlaylist);
        }

        public virtual bool Contains(Playlist item)
        {
            return playlists.Contains(item);
        }

        public virtual void CopyTo(Playlist[] array, int arrayIndex)
        {
            playlists.CopyTo(array, arrayIndex);
        }

        public virtual IEnumerator<Playlist> GetEnumerator()
        {
            return playlists.GetEnumerator();
        }

        public virtual int IndexOf(Playlist item)
        {
            return playlists.IndexOf(item);
        }

        public virtual void Insert(int index, Playlist item)
        {
            if (Contains(item)) return;

            Playlist currentPlaylist = Library.Current.CurrentPlaylist;

            playlists.Insert(index, item);

            Library.Data?.UpdateAddPlaylist(index, item, currentPlaylist);
        }

        public virtual bool Remove(Playlist item)
        {
            int index = IndexOf(item);
            Playlist currentPlaylist = Library.Current.CurrentPlaylist;

            if (!playlists.Remove(item)) return false;

            Library.Data?.UpdateRemovePlaylist(index, item, currentPlaylist);

            return true;
        }

        public virtual void RemoveAt(int index)
        {
            Playlist currentPlaylist = Library.Current.CurrentPlaylist;
            Playlist removePlaylist = this.ElementAtOrDefault(index);

            playlists.RemoveAt(index);

            Library.Data?.UpdateRemovePlaylist(index, removePlaylist, currentPlaylist);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return playlists.GetEnumerator();
        }

        protected virtual Playlist GetThis(int index)
        {
            return playlists[index];
        }

        protected virtual void SetThis(int index, Playlist playlist)
        {
            if (playlist == playlists[index]) return;

            Playlist currentPlaylist = Library.Current.CurrentPlaylist;
            Playlist removePlaylist = playlists[index];

            playlists[index] = playlist;

            Library.Data.UpdateAddRemovePlaylist(index, playlist, removePlaylist, currentPlaylist);
        }

        protected virtual int GetCount()
        {
            return playlists.Count;
        }
    }
}
