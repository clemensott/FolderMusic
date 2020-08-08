using System;
using MusicPlayer.Models.Enums;

namespace MusicPlayer.Models.EventArgs
{
    public class PlaylistReceivedEventArgs
    {
        public Song? CurrentSong { get; }

        public TimeSpan Position { get; }

        public LoopType Loop { get; }

        public Song[] Songs { get; }

        public PlaylistReceivedEventArgs(Song? currentSong, TimeSpan position, LoopType loop, Song[] songs)
        {
            CurrentSong = currentSong;
            Position = position;
            Loop = loop;
            Songs = songs;
        }
    }
}
