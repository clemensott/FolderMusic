namespace MusicPlayer.Models.EventArgs
{
    public class IsPlayingChangedEventArgs : System.EventArgs
    {
        public bool NewValue { get; private set; }

        internal IsPlayingChangedEventArgs(bool newValue)
        {
            NewValue = newValue;
        }
    }
}
