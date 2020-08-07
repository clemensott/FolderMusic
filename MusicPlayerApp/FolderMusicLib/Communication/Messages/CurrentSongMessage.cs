using System;
using MusicPlayer.Models;

namespace MusicPlayer.Communication.Messages
{
    public class CurrentSongMessage
    {
        public TimeSpan Position { get; set; }

        public Song? Song { get; set; }

        public CurrentSongMessage() { }

        public CurrentSongMessage(Song? song, TimeSpan position)
        {
            Position = position;
            Song = song;
        }
    }
}
