using System.Collections.Generic;

namespace MusicPlayer.Data
{
    class NonLoadedSongList : SongList
    {
        private static NonLoadedSongList instance;

        public static NonLoadedSongList Current
        {
            get
            {
                if (instance == null) instance = new NonLoadedSongList();

                return instance;
            }
        }

        private Song currentSong;

        private NonLoadedSongList()
        {
            currentSong = CurrentPlaySong.Current.Song;
            songs = new List<Song>() { currentSong };
        }

        public override void Add(Song song)
        {
        }

        public override void Clear()
        {
        }

        public override bool Contains(Song item)
        {
            return item == currentSong;
        }

        public override IEnumerator<Song> GetEnumerator()
        {
            return base.GetEnumerator();
        }

        public override int IndexOf(Song item)
        {
            return item == currentSong ? 0 : -1;
        }

        public override void Insert(int index, Song item)
        {
        }

        public override bool Remove(Song item)
        {
            return false;
        }

        public override void RemoveAt(int index)
        {
        }

        protected override Song GetThis(int index)
        {
            return currentSong;
        }

        protected override void SetThis(int index, Song song)
        {
            currentSong = song;
        }

        protected override int GetCount()
        {
            return songs.Count;
        }
    }
}
