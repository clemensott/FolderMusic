namespace MusicPlayerApp
{
    public class UiUpdate
    {
        public static void CurrentSongTitleArtistNaturalDuration()
        {
            ShuffleListIndex();
            App.ViewModel.NotifyPropertyChanged("CurrentSongTitle");
            App.ViewModel.NotifyPropertyChanged("CurrentSongArtist");

            CurrentSongNaturalDuration();
            CurrentSongPosition();
        }

        public static void CurrentSongNaturalDuration()
        {
            if (App.ViewModel.CurrentSongNaturalDurationMilliseconds < 2) return;

            LibraryLib.Library.Current.CurrentSong.NaturalDurationMilliseconds = App.ViewModel.CurrentSongNaturalDurationMilliseconds;

            App.ViewModel.NotifyPropertyChanged("CurrentSongNaturalDurationText");
            App.ViewModel.NotifyPropertyChanged("CurrentSongNaturalDurationMilliseconds");
        }

        public static void CurrentSongPosition()
        {
            App.ViewModel.NotifyPropertyChanged("CurrentSongPostionText");
            App.ViewModel.NotifyPropertyChanged("CurrentSongPositionMilliseconds");
        }

        public static void PlayPauseIcon()
        {
            App.ViewModel.NotifyPropertyChanged("PlayPauseIcon");
        }

        public static void ShuffleIcon()
        {
            App.ViewModel.NotifyPropertyChanged("ShuffleIcon");
        }

        public static void LoopIcon()
        {
            App.ViewModel.NotifyPropertyChanged("LoopIcon");
        }

        public static void Playlists()
        {
            App.ViewModel.NotifyPropertyChanged("Playlists");
        }

        public static void CurrentPlaylistIndex()
        {
            App.ViewModel.NotifyPropertyChanged("CurrentPlaylistIndex");
        }

        public static void CurrentPlaylistIndexAndRest()
        {
            App.ViewModel.NotifyPropertyChanged("CurrentPlaylistName");
            LoopIcon();
            ShuffleIcon();
            CurrentPlaylistSongs();
            CurrentSongTitleArtistNaturalDuration();
        }

        public static void CurrentPlaylistSongs()
        {
            App.ViewModel.NotifyPropertyChanged("CurrentPlaylistSongs");
        }

        public static void ShuffleListIndex()
        {
            App.ViewModel.NotifyPropertyChanged("ShuffleListIndex");
        }

        public static void AfterActivating()
        {
            CurrentSongTitleArtistNaturalDuration();
            PlayPauseIcon();
        }

        public static void PlaylistsAndCurrentPlaylist()
        {
            Playlists();
            CurrentPlaylistIndexAndRest();
        }

        public static void All()
        {
            App.ViewModel.NotifyPropertyChanged("CurrentPlaylistSongs");
            App.ViewModel.NotifyPropertyChanged("Playlists");

            App.ViewModel.NotifyPropertyChanged("ShuffleListIndex");
            App.ViewModel.NotifyPropertyChanged("CurrentPlaylistIndex");
            App.ViewModel.NotifyPropertyChanged("CurrentPlaylistName");

            App.ViewModel.NotifyPropertyChanged("CurrentSongTitle");
            App.ViewModel.NotifyPropertyChanged("CurrentSongArtist");

            App.ViewModel.NotifyPropertyChanged("CurrentSongPostionText");
            App.ViewModel.NotifyPropertyChanged("CurrentSongPositionMilliseconds");
            App.ViewModel.NotifyPropertyChanged("CurrentSongNaturalDurationText");
            App.ViewModel.NotifyPropertyChanged("CurrentSongNaturalDurationMilliseconds");

            App.ViewModel.NotifyPropertyChanged("LoopIcon");
            App.ViewModel.NotifyPropertyChanged("NextIcon");
            App.ViewModel.NotifyPropertyChanged("PlayPauseIcon");
            App.ViewModel.NotifyPropertyChanged("PreviousIcon");
            App.ViewModel.NotifyPropertyChanged("ShuffleIcon");
        }
    }
}
