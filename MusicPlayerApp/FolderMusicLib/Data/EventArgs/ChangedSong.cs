namespace MusicPlayer.Data
{
    public class ChangedSong
    {
        public int Index { get; private set; }

        public Song Song { get; private set; }

        public ChangedSong(int index, Song song)
        {
            Index = index;
            Song = song;
        }
    }
}
