using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models.EventArgs
{
    public class CurrentPlaylistChangedEventArgs : System.EventArgs
    {
        public IPlaylist OldCurrentPlaylist { get; private set; }

        public IPlaylist NewCurrentPlaylist { get; private set; }

        internal CurrentPlaylistChangedEventArgs(IPlaylist oldCurrentPlaylist, IPlaylist newCurrentPlaylist)
        {
            OldCurrentPlaylist = oldCurrentPlaylist;
            NewCurrentPlaylist = newCurrentPlaylist;
        }
    }
}
