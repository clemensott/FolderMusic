using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data
{
    public class SongList : IList<Song>
    {
        protected List<Song> songs;

        public Song this[int index]
        {
            get
            {
                return songs[index];
            }

            set
            {
                if (value == songs[index]) return;

                Playlist playlist = GetPlaylist();
                Song currentSong = playlist?.CurrentSong;
                Song removeSong = songs[index];

                songs[index] = value;

                playlist?.UpdateAddRemoveSong(index, value, removeSong, currentSong);
            }
        }

        public int Count
        {
            get
            {
                return songs.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public SongList()
        {
            songs = new List<Song>();
        }
        
        public SongList(IEnumerable<Song> songs)
        {
            this.songs = new List<Song>(songs);
        }

        public virtual void Add(Song item)
        {
            if (Contains(item)) return;

            IList<Song> oldSongs = this.ToList();
            Playlist playlist = GetPlaylist();
            Song currentSong = playlist?.CurrentSong;
            int index = Count;

            songs.Add(item);

            playlist?.UpdateAddSong(index, item, oldSongs, currentSong);
        }

        public virtual void Clear()
        {
            songs.Clear();

            Playlist playlist = GetPlaylist();

            Library.Current.Playlists.Remove(playlist);
        }

        public virtual bool Contains(Song item)
        {
            return songs.Contains(item);
        }

        public void CopyTo(Song[] array, int arrayIndex)
        {
            songs.CopyTo(array, arrayIndex);
        }

        public virtual IEnumerator<Song> GetEnumerator()
        {
            return songs.GetEnumerator();
        }

        public virtual int IndexOf(Song item)
        {
            return songs.IndexOf(item);
        }

        public virtual void Insert(int index, Song item)
        {
            if (Contains(item)) return;

            IList<Song> oldSongs = songs.ToList();
            Playlist playlist = GetPlaylist();
            Song currentSong = playlist?.CurrentSong;

            songs.Insert(index, item);

            playlist?.UpdateAddSong(index, item, oldSongs, currentSong);
        }

        public virtual bool Remove(Song item)
        {
            int index = IndexOf(item);
            List<Song> oldSongs = this.ToList();
            Playlist playlist = GetPlaylist();
            Song currentSong = playlist?.CurrentSong;

            if (!songs.Remove(item)) return false;
            if (playlist == null) return true;
            if (playlist.IsEmptyOrLoading) return true;

            if (Count == 0)
            {
                Library.Current.Playlists.Remove(playlist);
                return true;
            }

            playlist.UpdateRemoveSong(index, item, currentSong);

            return true;
        }

        public virtual void RemoveAt(int index)
        {
            Song item = this.ElementAtOrDefault(index);
            List<Song> oldSongs = this.ToList();
            Playlist playlist = GetPlaylist();
            Song currentSong = playlist?.CurrentSong;

            songs.RemoveAt(index);

            if (playlist == null) return;
            if (playlist.IsEmptyOrLoading) return;

            if (Count == 0)
            {
                Library.Current.Playlists.Remove(playlist);
                return;
            }

            playlist.UpdateRemoveSong(index, item, currentSong);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return songs.GetEnumerator();
        }

        private Playlist GetPlaylist()
        {
            return Library.IsLoaded(this) ? Library.Current.Playlists.FirstOrDefault(p => p.Songs == this) : null;
        }

        protected virtual Song GetThis(int index)
        {
            return songs[index];
        }

        protected virtual void SetThis(int index, Song song)
        {
            if (song == songs[index]) return;

            Playlist playlist = GetPlaylist();
            Song currentSong = playlist?.CurrentSong;
            Song removeSong = songs[index];

            songs[index] = song;

            playlist?.UpdateAddRemoveSong(index, song, removeSong, currentSong);
        }

        protected virtual int GetCount()
        {
            return songs.Count;
        }
    }
}
