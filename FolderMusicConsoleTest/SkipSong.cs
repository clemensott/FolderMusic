namespace MusicPlayer.Data
{
    public enum ProgressType { Remove, Leave, Skip }

    public class SkipSong
    {
        public ProgressType Handle { get; set; }

        public Song Song { get; private set; }

        public SkipSong(Song song)
        {
            Handle = ProgressType.Leave;
            Song = song;
        }
    }
}
