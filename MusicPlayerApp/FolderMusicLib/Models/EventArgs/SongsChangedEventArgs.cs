using MusicPlayer.Models.Foreground.Interfaces;

namespace MusicPlayer.Models.EventArgs
{
    public class SongsChangedEventArgs : System.EventArgs
    {
        public ISongCollection OldSongs { get; private set; }

        public ISongCollection NewSongs { get; private set; }

        public SongsChangedEventArgs(ISongCollection oldSongs, ISongCollection newSongs)
        {
            OldSongs = oldSongs;
            NewSongs = newSongs;
        }
    }
}