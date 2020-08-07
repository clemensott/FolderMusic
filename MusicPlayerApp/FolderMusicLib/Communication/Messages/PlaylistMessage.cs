using System;
using MusicPlayer.Models;
using MusicPlayer.Models.Enums;

namespace MusicPlayer.Communication.Messages
{
    public class PlaylistMessage
    {
        public long PositionTicks { get; set; }
        
        public Song ? CurrentSong { get; set; }
        
        public LoopType Loop { get; set; }
        
        public Song[] Songs { get; set; }
    }
}
