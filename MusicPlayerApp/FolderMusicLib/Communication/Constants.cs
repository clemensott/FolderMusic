namespace MusicPlayer.Communication
{
    enum ForegroundMessageType : byte
    {
        SetCurrentSong,
        SetPosition,
        SetPlaylist,
        SetSongs,
        SetLoop,
        Play,
        Pause,
        Next,
        Previous,
    }

    enum BackgroundMessageType
    {
        SetCurrentSong,
        SetIsPlaying,
    }

    static class Constants
    {
        public const string TypeKey = "TYPE", ValueKey = "VALUE";
    }
}
