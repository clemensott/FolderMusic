namespace MusicPlayer.Data
{
    public class SongsChangedEventArgs
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