using MusicPlayer.Data;

namespace FolderMusic.FrameHistory.Handlers
{
    class SkippedSongsPageHandler : HistoricFrameHandler
    {
        public override HistoricParameter ToHistoricParameter(object parameter)
        {
            return new HistoricParameter();
        }

        public override Parameter FromHistoricParameter(HistoricParameter parameter, ILibrary library)
        {
            return new Parameter(library.SkippedSongs);
        }
    }
}
