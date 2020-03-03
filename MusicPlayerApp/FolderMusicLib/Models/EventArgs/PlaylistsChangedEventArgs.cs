using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models.EventArgs
{
    public class PlaylistsChangedEventArgs : System.EventArgs
    {
        public IPlaylistCollection OldPlaylists { get; private set; }

        public IPlaylistCollection NewPlaylists { get; private set; }

        public PlaylistsChangedEventArgs(IPlaylistCollection oldPlaylists, IPlaylistCollection newPlaylists)
        {
            OldPlaylists = oldPlaylists;
            NewPlaylists = newPlaylists;
        }
    }
}