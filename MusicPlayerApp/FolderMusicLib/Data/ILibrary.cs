using System.Threading.Tasks;

namespace MusicPlayer.Data
{
    public interface ILibrary
    {
        Playlist this[int index] { get; set; }

        bool CanceledLoading { get; }
        Playlist CurrentPlaylist { get; set; }
        int CurrentPlaylistIndex { get; set; }
        bool IsEmpty { get; }
        bool IsPlaying { get; set; }
        int Length { get; }
        PlaylistList Playlists { get; set; }
        SkipSongs SkippedSongs { get; }

        Task AddNotExistingPlaylists();
        void CancelLoading();
        int GetPlaylistIndex(Playlist playlist);
        Task RefreshLibraryFromStorage();
        void Save();
        Task SaveAsync();
        Task UpdateExistingPlaylists();
    }
}