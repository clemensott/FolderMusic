namespace PlaylistSong
{
    public class SavePlaylist
    {
        public int CurrentSongIndex;
        public string Name, Path;
        public SaveSong[] Songs;
        public int[] ShuffleList;
        public ShuffleKind Shuffle;
        public LoopKind Loop;

        public SavePlaylist() { }

        public SavePlaylist(Playlist playlist)
        {
            CurrentSongIndex = playlist.CurrentSongIndex;
            Name = playlist.Name;
            Path = playlist.AbsolutePath;
            Songs = new SaveSong[playlist.Lenght];

            for (int i = 0; i < playlist.Lenght; i++)
            {
                Songs[i] = new SaveSong(playlist[i]);
            }

            ShuffleList = playlist.ShuffleList.ToArray();
            Shuffle = playlist.Shuffle;
            Loop = playlist.Loop;
        }
    }
}
