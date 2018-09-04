using System;

namespace MusicPlayer.Data
{
    public class CurrentPlaylistChangedEventArgs : EventArgs
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
