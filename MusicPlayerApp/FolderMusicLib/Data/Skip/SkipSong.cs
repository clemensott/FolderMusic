namespace MusicPlayer.Data
{
    public enum HandleType { Remove, Keep, Skip }

    public class SkipSong
    {
        public HandleType Handle { get; set; }

        public Song Song { get; private set; }

        public SkipSong(Song song)
        {
            Handle = HandleType.Keep;
            Song = song;
        }
    }
}
