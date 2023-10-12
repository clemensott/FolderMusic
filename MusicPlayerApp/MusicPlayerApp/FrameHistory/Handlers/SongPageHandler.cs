using System;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using FolderMusic.NavigationParameter;
using MusicPlayer;
using MusicPlayer.Handler;
using MusicPlayer.Models;
using MusicPlayer.Models.Foreground.Interfaces;

namespace FolderMusic.FrameHistory.Handlers
{
    class SongPageHandler : HistoricFrameHandler
    {
        public override HistoricParameter ToHistoricParameter(object parameter)
        {
            string songPath = ((SongPageParameter)parameter).Song.FullPath;

            return new HistoricParameter(songPath, true);
        }

        public override object ToHistoricDataContext(object dataContext)
        {
            return new RestoreMusicProperties((MusicProperties)dataContext);
        }

        public override Parameter FromHistoricParameter(HistoricParameter parameter, ForegroundPlayerHandler handler)
        {
            string songPath = (string)parameter.Value;
            foreach (IPlaylist playlist in handler.Library.Playlists)
            {
                Song song;

                if (!playlist.Songs.TryGetSong(songPath, out song)) continue;

                RestoreMusicProperties rmp = (RestoreMusicProperties)parameter.DataContext;
                Task<MusicProperties> task = rmp.ToMusicProperties(songPath);

                task.Wait();

                return new Parameter(new SongPageParameter(song, playlist.Songs), task.Result);
            }

            throw new Exception($"Song '{songPath}' not found for SongPage restore");
        }
    }
}
