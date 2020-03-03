namespace MusicPlayer.Models.EventArgs
{
    public class SongArtistChangedEventArgs : System.EventArgs
    {
        public string OldArtist { get; private set; }

        public string NewArtist { get; private set; }

        internal SongArtistChangedEventArgs(string oldArtist, string newArtist)
        {
            OldArtist = oldArtist;
            NewArtist = newArtist;
        }
    }
}
