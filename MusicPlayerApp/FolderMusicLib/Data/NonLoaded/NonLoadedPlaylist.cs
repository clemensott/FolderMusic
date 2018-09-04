using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicPlayer.Data
{
    class NonLoadedPlaylist : Playlist
    {
        private static NonLoadedPlaylist instance;

        public static NonLoadedPlaylist Current
        {
            get
            {
                if (instance == null) instance = new NonLoadedPlaylist();

                return instance;
            }
        }

        private NonLoadedPlaylist()
        {
            Name = "Loading";

            Loop = LoopType.Off;
            Shuffle = ShuffleType.Off;

            songs = NonLoadedSongList.Current;
            shuffleList = new List<int>() { 0 };

            songsIndex = 0;
            songPositionPercent = CurrentPlaySong.Current.PositionPercent;
        }

        public override bool ChangeCurrentSong(int offset)
        {
            return true;
        }

        protected override bool GetIsEmptyOrLoading()
        {
            return true;
        }

        protected override int GetPlaylistIndex()
        {
            return -1;
        }

        protected override int GetShuffleListIndex()
        {
            return 0;
        }

        protected override SongList GetSongs()
        {
            return songs;
        }

        protected override int GetSongsIndex()
        {
            return 0;
        }

        protected override void SetShuffle(ShuffleType value)
        {
        }

        protected override void SetLoop(LoopType value)
        {
        }

        protected override void SetShuffleListIndex(int value)
        {
        }

        protected override void SetSongIndex(int newSongIndex)
        {
        }

        protected override void SetSongs(SongList newSongs)
        {
        }

        public override async Task LoadSongsFromStorage()
        {
        }

        public override async Task SearchForNewSongs()
        {
        }

        public override async Task UpdateSongsFromStorage()
        {
        }
    }
}
