using MusicPlayer.Models;

namespace FolderMusic.EventArgs
{
    public class SelectedSongChangedManuallyEventArgs : System.EventArgs
    {
        public Song? OldCurrentSong { get; }

        public Song? NewCurrentSong { get; }

        public SelectedSongChangedManuallyEventArgs(Song? oldCurrentSong, Song? newCurrentSong)
        {
            OldCurrentSong = oldCurrentSong;
            NewCurrentSong = newCurrentSong;
        }
    }
}
