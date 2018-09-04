using System.Collections.Generic;

namespace MusicPlayer.Data
{
    class NonLoadedPlaylistList : PlaylistList
    {
        private static NonLoadedPlaylistList instance;

        public static NonLoadedPlaylistList Current
        {
            get
            {
                if (instance == null) instance = new NonLoadedPlaylistList();

                return instance;
            }
        }

        private Playlist nonLoadPlaylist;

        private NonLoadedPlaylistList()
        {
            nonLoadPlaylist = NonLoadedPlaylist.Current;
            playlists = new List<Playlist>() { nonLoadPlaylist };
        }

        public override void Add(Playlist item)
        {
        }

        public override void Clear()
        {
        }

        public override bool Contains(Playlist item)
        {
            return item == nonLoadPlaylist;
        }

        public override void CopyTo(Playlist[] array, int arrayIndex)
        {
            playlists.CopyTo(array, arrayIndex);
        }

        public override IEnumerator<Playlist> GetEnumerator()
        {
            return playlists.GetEnumerator();
        }

        public override int IndexOf(Playlist item)
        {
            return playlists.IndexOf(item);
        }

        public override void Insert(int index, Playlist item)
        {
        }

        public override bool Remove(Playlist item)
        {
            return false;
        }

        public override void RemoveAt(int index)
        {
        }

        protected override Playlist GetThis(int index)
        {
            return nonLoadPlaylist;
        }

        protected override void SetThis(int index, Playlist playlist)
        {
            nonLoadPlaylist = playlist;
        }

        protected override int GetCount()
        {
            return 1;
        }
    }
}
