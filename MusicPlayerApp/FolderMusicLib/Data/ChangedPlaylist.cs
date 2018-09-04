namespace MusicPlayer.Data
{
    public class ChangedPlaylist
    {
        public int Index { get; private set; }

        public Playlist Playlist { get; private set; }

        public ChangedPlaylist(int index, Playlist playlist)
        {
            Index = index;
            Playlist = playlist;
        }
    }
}
