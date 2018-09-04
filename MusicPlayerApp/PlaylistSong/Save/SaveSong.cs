namespace PlaylistSong
{
    public class SaveSong
    {
        public double NaturalDurationMilliseconds;
        public string Title, Artist, Path;

        public SaveSong() { }

        public SaveSong(Song song)
        {
            Title = song.Title;
            Artist = song.Artist;
            Path = song.Path;
            NaturalDurationMilliseconds = song.NaturalDurationMilliseconds;
        }
    }
}
