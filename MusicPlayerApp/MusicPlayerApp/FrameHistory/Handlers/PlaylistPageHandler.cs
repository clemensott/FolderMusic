using System.Linq;
using FolderMusic.NavigationParameter;
using MusicPlayer.Handler;
using MusicPlayer.Models.Foreground.Interfaces;

namespace FolderMusic.FrameHistory.Handlers
{
    class PlaylistPageHandler : HistoricFrameHandler
    {
        public override HistoricParameter ToHistoricParameter(object parameter)
        {
            string playlistPath = ((PlaylistPageParameter)parameter).Playlist.AbsolutePath;

            return new HistoricParameter(playlistPath);
        }

        public override Parameter FromHistoricParameter(HistoricParameter parameter, ForegroundPlayerHandler handler)
        {
            string playlistPath = (string)parameter.Value;
            IPlaylist playlist = handler.Library.Playlists.First(p => p.AbsolutePath == playlistPath);

            return new Parameter(new PlaylistPageParameter(handler, playlist));
        }
    }
}
