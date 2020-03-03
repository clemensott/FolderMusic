namespace MusicPlayer.Models.EventArgs
{
    public class CurrentSongPositionChangedEventArgs : System.EventArgs
    {
        public double OldCurrentSongPosition { get; private set; }

        public double NewCurrentSongPosition { get; private set; }

        internal CurrentSongPositionChangedEventArgs(double oldCurrentSongPosition, double newCurrentSongPosition)
        {
            OldCurrentSongPosition = oldCurrentSongPosition;
            NewCurrentSongPosition = newCurrentSongPosition;
        }
    }
}
