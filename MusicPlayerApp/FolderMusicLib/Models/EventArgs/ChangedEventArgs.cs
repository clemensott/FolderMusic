namespace MusicPlayer.Models.EventArgs
{
    public class ChangedEventArgs<TValue> : System.EventArgs
    {
        public TValue OldValue { get; }

        public TValue NewValue { get; }

        public ChangedEventArgs(TValue oldValue, TValue newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
