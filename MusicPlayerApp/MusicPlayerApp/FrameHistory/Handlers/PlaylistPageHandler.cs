using System.Linq;
using MusicPlayer.Models.Interfaces;

namespace FolderMusic.FrameHistory.Handlers
{
    class PlaylistPageHandler : HistoricFrameHandler
    {
        public override HistoricParameter ToHistoricParameter(object parameter)
        {
            string playlistPath = ((IPlaylist)parameter).AbsolutePath;

            return new HistoricParameter(playlistPath);
        }

        public override Parameter FromHistoricParameter(HistoricParameter parameter, ILibrary library)
        {
            string playlistPath = (string)parameter.Value;

            return new Parameter(library.Playlists.First(p => p.AbsolutePath == playlistPath));
        }
    }
}
