using MusicPlayer.Models.Interfaces;

namespace FolderMusic.FrameHistory.Handlers
{
    class MainPageHandler : HistoricFrameHandler
    {
        public override HistoricParameter ToHistoricParameter(object parameter)
        {
            return new HistoricParameter();
        }

        public override Parameter FromHistoricParameter(HistoricParameter parameter, ILibrary library)
        {
            return new Parameter(library);
        }
    }
}
