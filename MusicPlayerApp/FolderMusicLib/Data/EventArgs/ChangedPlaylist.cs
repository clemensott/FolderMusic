namespace MusicPlayer.Data
{
    public class ChangedPlaylist
    {
        public int Index { get; private set; }

        public IPlaylist Playlist { get; private set; }

        public ChangedPlaylist(int index, IPlaylist playlist)
        {
            Index = index;
            Playlist = playlist;
        }
    }
}
