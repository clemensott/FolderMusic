using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using MusicPlayer.Data.SubscriptionsHandler;
using System;
using System.ComponentModel;

namespace FolderMusic.ViewModels
{
    public class CurrentSongViewModel : INotifyPropertyChanged
    {
        private ILibrary library;
        private LibrarySubscriptionsHandler lsh;

        public string Artist
        {
            get { return library?.CurrentPlaylist?.CurrentSong?.Artist; }
            set
            {
                if (library?.CurrentPlaylist?.CurrentSong != null)
                {
                    library.CurrentPlaylist.CurrentSong.Artist = value;
                }
            }
        }

        public string Title
        {
            get { return library?.CurrentPlaylist?.CurrentSong?.Title; }
            set
            {
                if (library?.CurrentPlaylist?.CurrentSong != null)
                {
                    library.CurrentPlaylist.CurrentSong.Title = value;
                }
            }
        }

        public double PositionRatio
        {
            get { return library?.CurrentPlaylist?.CurrentSongPosition ?? 0; }
            set { if (library?.CurrentPlaylist != null) library.CurrentPlaylist.CurrentSongPosition = value; }
        }

        public TimeSpan Position
        {
            get { return TimeSpan.FromDays(PositionRatio * Duration.TotalDays); }
            set { if (Duration.TotalDays > 0) library.CurrentPlaylist.CurrentSongPosition = value.TotalDays / Duration.TotalDays; }
        }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromMilliseconds(library?.CurrentPlaylist?.CurrentSong?.DurationMilliseconds ?? 0); }
            set
            {
                if (library?.CurrentPlaylist?.CurrentSong != null)
                {
                    library.CurrentPlaylist.CurrentSong.DurationMilliseconds = value.TotalMilliseconds;
                }
            }
        }

        public LoopType Loop
        {
            get { return library?.CurrentPlaylist?.Loop ?? LoopType.Off; }
            set { if (library?.CurrentPlaylist != null) library.CurrentPlaylist.Loop = value; }
        }

        public ShuffleType Shuffle
        {
            get { return library.CurrentPlaylist.Songs?.Shuffle?.Type ?? ShuffleType.Off; }
            set
            {
                ISongCollection songs = library?.CurrentPlaylist.Songs;
                if (songs?.Shuffle != null && value != songs.Shuffle.Type) songs.SetShuffleType(value);
            }
        }

        public CurrentSongViewModel(ILibrary library)
        {
            this.library = library;
            lsh = LibrarySubscriptionsHandler.GetInstance(library);

            lsh.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            lsh.CurrentPlaylist.CurrentSong.ArtistChanged += OnArtistChanged;
            lsh.CurrentPlaylist.CurrentSong.TitleChanged += OnTitleChanged;
            lsh.CurrentPlaylist.CurrentSong.DurationChanged += OnDurationChanged;
            lsh.CurrentPlaylist.CurrentSongChanged += OnCurrentSongChanged;
            lsh.CurrentPlaylist.CurrentSongPositionChanged += OnPositionChanged;
            lsh.CurrentPlaylist.LoopChanged += OnLoopChanged;
            lsh.CurrentPlaylist.ShuffleChanged += onShuffleChanged;
        }

        private void OnCurrentPlaylistChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Artist));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(PositionRatio));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Duration));
            OnPropertyChanged(nameof(Loop));
            OnPropertyChanged(nameof(Shuffle));
        }

        private void OnArtistChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Artist));
        }

        private void OnTitleChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Title));
        }

        private void OnDurationChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(PositionRatio));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Duration));
        }

        private void OnCurrentSongChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Artist));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(PositionRatio));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Duration));
        }

        private void OnPositionChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(PositionRatio));
            OnPropertyChanged(nameof(Position));
        }

        private void OnLoopChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Loop));
        }

        private void onShuffleChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Shuffle));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
