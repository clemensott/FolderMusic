using System;
using Windows.Media.Playback;

namespace MusicPlayer.Data
{
    public class PlayerStateChangedEventArgs : EventArgs
    {
        public MediaPlayerState OldState { get; private set; }

        public MediaPlayerState NewState { get; private set; }

        public PlayerStateChangedEventArgs(MediaPlayerState oldState, MediaPlayerState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
}