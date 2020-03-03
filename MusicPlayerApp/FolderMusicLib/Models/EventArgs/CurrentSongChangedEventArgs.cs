namespace MusicPlayer.Models.EventArgs
{
    public class CurrentSongChangedEventArgs : System.EventArgs
    {
        public Song OldCurrentSong { get; private set; }

        public Song NewCurrentSong { get; private set; }

        internal CurrentSongChangedEventArgs(Song oldCurrentSong, Song newCurrentSong)
        {
            OldCurrentSong = oldCurrentSong;
            NewCurrentSong = newCurrentSong;
        }
    }
}
