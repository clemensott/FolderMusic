namespace MusicPlayer.Models.EventArgs
{
    public class SongTitleChangedEventArgs : System.EventArgs
    {
        public string OldTitle { get; private set; }

        public string NewTitle { get; private set; }

        internal SongTitleChangedEventArgs(string oldTitle, string newTitle)
        {
            OldTitle = oldTitle;
            NewTitle = newTitle;
        }
    }
}
