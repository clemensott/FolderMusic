namespace MusicPlayerApp
{
    public class UiUpdate
    {
        public static void CurrentSongTitleArtistNaturalDuration()
        {
            CurrentSongIndex();
            App.ViewModel.NotifyPropertyChanged("CurrentSongTitle");
            App.ViewModel.NotifyPropertyChanged("CurrentSongArtist");

            CurrentSongNaturalDuration();
            CurrentSongPosition();
        }

        public static void CurrentSongNaturalDuration()
        {
            if (App.ViewModel.CurrentSongNaturalDurationMilliseconds < 2) return;

            App.ViewModel.CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds = App.ViewModel.CurrentSongNaturalDurationMilliseconds;

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

        public static void PlaylistsAndCurrentPlaylistIndex()
        {
            App.ViewModel.NotifyPropertyChanged("Playlists");
            CurrentPlaylistIndex();
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
            CurrentSongTitleArtistNaturalDuration();
            CurrentPlaylistSongsAndIndex();
        }

        public static void CurrentPlaylistSongsAndIndex()
        {
            App.ViewModel.NotifyPropertyChanged("CurrentPlaylistSongs");
            CurrentPlaylistIndex();
        }

        public static void CurrentSongIndex()
        {
            App.ViewModel.NotifyPropertyChanged("CurrentSongIndex");
        }

        public static void AfterActivating()
        {
            CurrentSongTitleArtistNaturalDuration();
            PlayPauseIcon();
        }

        public static void AfterDeleteCurrentPlaylist()
        {
            PlaylistsAndCurrentPlaylistIndex();
            CurrentPlaylistIndexAndRest();
        }

        public static void All()
        {
            App.ViewModel.NotifyPropertyChanged("CurrentPlaylistSongs");
            App.ViewModel.NotifyPropertyChanged("Playlists");

            App.ViewModel.NotifyPropertyChanged("CurrentSongIndex");
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

            App.ViewModel.NotifyPropertyChanged("IsUiEnabled");
        }
    }
}
