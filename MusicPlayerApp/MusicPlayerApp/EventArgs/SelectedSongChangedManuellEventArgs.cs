using System;
using MusicPlayer.Models;

namespace FolderMusic
{
    public class SelectedSongChangedManuallyEventArgs:EventArgs
    {
        public Song OldCurrentSong { get; private set; }

        public Song NewCurrentSong { get; private set; }

        public SelectedSongChangedManuallyEventArgs(Song oldCurrentSong,Song newCurrentSong)
        {
            OldCurrentSong = oldCurrentSong;
            NewCurrentSong = newCurrentSong;
        }
    }
}