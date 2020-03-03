using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using MusicPlayer.Models;
using MusicPlayer.Models.Interfaces;

namespace FolderMusic.FrameHistory.Handlers
{
    class SongPageHandler : HistoricFrameHandler
    {
        public override HistoricParameter ToHistoricParameter(object parameter)
        {
            string songPath = ((Song)parameter).Path;

            return new HistoricParameter(songPath, true);
        }

        public override object ToHistoricDataContext(object dataContext)
        {
            return new RestoreMusicProperties((MusicProperties)dataContext);
        }

        public override Parameter FromHistoricParameter(HistoricParameter parameter, ILibrary library)
        {
            string songPath = (string)parameter.Value;
            Song song = library.Playlists.SelectMany(p => p.Songs).First(s => s.Path == songPath);

            RestoreMusicProperties rmp = (RestoreMusicProperties)parameter.DataContext;
            Task<MusicProperties> task = rmp.ToMusicProperties(songPath);

            task.Wait();

            return new Parameter(song, task.Result);
        }
    }
}
