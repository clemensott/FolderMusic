using MusicPlayer.Handler;
using MusicPlayer.Models.Interfaces;

namespace FolderMusic.FrameHistory.Handlers
{
    class SkippedSongsPageHandler : HistoricFrameHandler
    {
        public override HistoricParameter ToHistoricParameter(object parameter)
        {
            return new HistoricParameter();
        }

        public override Parameter FromHistoricParameter(HistoricParameter parameter, ForegroundPlayerHandler handler)
        {
            return new Parameter(handler.Library.SkippedSongs);
        }
    }
}
