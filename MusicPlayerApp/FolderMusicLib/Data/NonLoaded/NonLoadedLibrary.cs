using System.Threading.Tasks;

namespace MusicPlayer.Data
{
    class NonLoadedLibrary : LibraryBase
    {
        private static NonLoadedLibrary instance;

        public static NonLoadedLibrary Current
        {
            get
            {
                if (instance == null) instance = new NonLoadedLibrary();

                return instance;
            }
        }

        private NonLoadedLibrary()
        {
        }

        public override async Task AddNotExistingPlaylists()
        {
        }

        public override void CancelLoading()
        {
        }

        public override async Task RefreshLibraryFromStorage()
        {
        }

        public override void Save()
        {
        }

        public override async Task SaveAsync()
        {
        }

        public override async Task UpdateExistingPlaylists()
        {
        }

        protected override bool GetIsPlaying()
        {
            return false;
        }

        protected override void SetIsPlaying(bool value)
        {
        }

        protected override PlaylistList GetPlaylists()
        {
            return NonLoadedPlaylistList.Current;
        }

        protected override void SetPlaylists(PlaylistList newPlaylists)
        {
        }

        protected override int GetCurrentPlaylistIndex()
        {
            return 0;
        }

        protected override void SetCurrentPlaylistIndex(int newCurrentPlaylistIndex)
        {
        }

        protected override void SetCurrentPlaylist(Playlist newCurrentPlaylist)
        {
        }

        internal override string GetXmlText()
        {
            return "LibraryEmpty";
        }
    }
}
