using MusicPlayer.Handler;
using MusicPlayer.Models.Foreground.Interfaces;

namespace FolderMusic.NavigationParameter
{
    public class PlaylistPageParameter
    {
        public ForegroundPlayerHandler Handler { get; }
        
        public IPlaylist Playlist { get; }

        public PlaylistPageParameter(ForegroundPlayerHandler handler, IPlaylist playlist)
        {
            Handler = handler;
            Playlist = playlist;
        }
    }
}
