using System.Linq;
using System.Threading.Tasks;

namespace MusicPlayer.Data
{
    public abstract class LibraryBase : ILibrary
    {
        internal static bool? isForeground = null;

        protected bool isPlayling;
        protected volatile bool cancelLoading = false;
        protected int currentPlaylistIndex = 0;
        protected PlaylistList playlists;

        public bool IsPlaying
        {
            get { return GetIsPlaying(); }
            set { SetIsPlaying(value); }
        }

        public bool CanceledLoading { get { return cancelLoading; } }

        public bool IsEmpty { get { return playlists.Count == 0 || playlists.First().IsEmptyOrLoading; ; } }

        public PlaylistList Playlists
        {
            get { return GetPlaylists(); }
            set { SetPlaylists(value); }
        }

        public Playlist this[int index]
        {
            get { return Playlists[index]; }
            set { Playlists[index] = value; }
        }

        public SkipSongs SkippedSongs { get { return SkipSongs.Instance; } }

        public int Length { get { return Playlists.Count; } }

        public int CurrentPlaylistIndex
        {
            get { return GetCurrentPlaylistIndex(); }
            set { SetCurrentPlaylistIndex(value); }
        }

        public Playlist CurrentPlaylist
        {
            get { return Playlists[CurrentPlaylistIndex]; }
            set { SetCurrentPlaylist(value); }
        }

        protected abstract bool GetIsPlaying();
        protected abstract void SetIsPlaying(bool value);
        protected abstract PlaylistList GetPlaylists();
        protected abstract void SetPlaylists(PlaylistList newPlaylists);
        protected abstract int GetCurrentPlaylistIndex();
        protected abstract void SetCurrentPlaylistIndex(int newCurrentPlaylistIndex);
        protected abstract void SetCurrentPlaylist(Playlist newCurrentPlaylist);

        internal abstract string GetXmlText();

        public abstract Task AddNotExistingPlaylists();
        public abstract void CancelLoading();
        public abstract Task RefreshLibraryFromStorage();
        public abstract void Save();
        public abstract Task SaveAsync();
        public abstract Task UpdateExistingPlaylists();

        public int GetPlaylistIndex(Playlist playlist)
        {
            return Playlists.IndexOf(playlist);
        }


        internal bool HavePlaylistIndex(string playlistAbsolutePath, out int playlistIndex)
        {
            Playlist[] playlists = Playlists.Where(x => x.AbsolutePath == playlistAbsolutePath).ToArray();
            Playlist playlist = playlists.FirstOrDefault(p => p.AbsolutePath == playlistAbsolutePath);

            playlistIndex = -1;

            if (playlist == null) return false;

            playlistIndex = Playlists.IndexOf(playlist);
            return true;
        }

        internal bool HavePlaylistIndexAndSongsIndex(string songPath, out int playlistIndex, out int songsIndex)
        {
            for (playlistIndex = 0; playlistIndex < Length; playlistIndex++)
            {
                Song song = this[playlistIndex].Songs.FirstOrDefault(x => x.Path == songPath);

                if (song != null)
                {
                    songsIndex = Playlists[playlistIndex].Songs.IndexOf(song);
                    return true;
                }
            }

            playlistIndex = -1;
            songsIndex = -1;

            return false;
        }

        internal bool HavePlaylistIndexAndSongsIndex(Song song, out int playlistIndex, out int songsIndex)
        {
            for (playlistIndex = 0; playlistIndex < Length; playlistIndex++)
            {
                songsIndex = Playlists[playlistIndex].Songs.IndexOf(song);

                if (songsIndex != -1) return true;
            }

            playlistIndex = -1;
            songsIndex = -1;

            return false;
        }

    }
}