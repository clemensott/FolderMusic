using System;
using MusicPlayer.Models;

namespace MusicPlayer.Communication.Messages
{
    public class CurrentSongMessage
    {
        public long PositionTicks { get; set; }

        public Song? Song { get; set; }

        public CurrentSongMessage() { }

        public CurrentSongMessage(Song? song, long positionTicks)
        {
            PositionTicks = positionTicks;
            Song = song;
        }
    }
}
