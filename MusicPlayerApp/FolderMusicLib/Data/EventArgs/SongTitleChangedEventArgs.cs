using System;

namespace MusicPlayer.Data
{
    public class SongTitleChangedEventArgs : EventArgs
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
