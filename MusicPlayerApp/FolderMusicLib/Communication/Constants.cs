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
        Ping,
    }

    enum BackgroundMessageType
    {
        SetCurrentSong,
        SetIsPlaying,
        Ping,
    }

    static class Constants
    {
        public const string TypeKey = "TYPE", ValueKey = "VALUE";
    }
}
