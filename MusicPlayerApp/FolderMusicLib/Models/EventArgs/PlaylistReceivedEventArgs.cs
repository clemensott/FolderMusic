using System;
using MusicPlayer.Models.Enums;

namespace MusicPlayer.Models.EventArgs
{
    public class PlaylistReceivedEventArgs
    {
        public Song? CurrentSong { get; }

        public TimeSpan Position { get; }

        public double PlaybackRate { get; }

        public LoopType Loop { get; }

        public Song[] Songs { get; }

        public PlaylistReceivedEventArgs(Song? currentSong, TimeSpan position, double playbackRate, LoopType loop, Song[] songs)
        {
            CurrentSong = currentSong;
            Position = position;
            PlaybackRate = playbackRate;
            Loop = loop;
            Songs = songs;
        }
    }
}
