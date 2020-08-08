using MusicPlayer.Handler;

namespace FolderMusic.FrameHistory.Handlers
{
    class MainPageHandler : HistoricFrameHandler
    {
        public override HistoricParameter ToHistoricParameter(object parameter)
        {
            return new HistoricParameter();
        }

        public override Parameter FromHistoricParameter(HistoricParameter parameter, ForegroundPlayerHandler handler)
        {
            return new Parameter(handler);
        }
    }
}
