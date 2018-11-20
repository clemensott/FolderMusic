using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System.ComponentModel;

namespace FolderMusic
{
    class PlaylistViewModel : INotifyPropertyChanged
    {
        private IPlaylist source;

        public double CurrentSongPosition
        {
            get { return Source?.CurrentSongPosition ?? 0; }
            set { if (Source != null) Source.CurrentSongPosition = value; }
        }

        public double CurrentSongPositionMillis
        {
            get { return CurrentSongPosition * CurrentSongDuration; }
            set
            {
                if (Source == null || CurrentSongDuration == 0) return;

                Source.CurrentSongPosition = value / CurrentSongDuration;
            }
        }

        public double CurrentSongDuration
        {
            get { return CurrentSong?.DurationMilliseconds ?? 0; }
            set { if (CurrentSong == null) CurrentSong.DurationMilliseconds = value; }
        }

        public string Name { get { return Source?.Name ?? "Empty"; } }

        public string AbsolutePath { get { return Source?.AbsolutePath ?? "None"; } }

        public string CurrentSongTitle { get { return CurrentSong?.Title; } }

        public string CurrentSongArtist { get { return CurrentSong?.Artist; } }

        public Song CurrentSong
        {
            get { return Source?.CurrentSong; }
            set { if (Source == null) Source.CurrentSong = value; }
        }

        public LoopType Loop
        {
            get { return Source?.Loop ?? LoopType.Off; }
            set { if (Source != null) Source.Loop = value; }
        }

        public ShuffleType Shuffle
        {
            get { return Songs?.Shuffle?.Type ?? ShuffleType.Off; }
            set
            {
                if (Songs?.Shuffle != null && value == Songs.Shuffle.Type) return;

                Songs.SetShuffleType(value);
                OnPropertyChanged("Shuffle");
            }
        }

        public ISongCollection Songs { get { return Source?.Songs; } }

        public IPlaylist Source
        {
            get { return source; }
            set
            {
                if (value == source) return;

                Unsubscribe(source);
                source = value;
                Subscribe(source);

                OnPropertyChanged("Source");
            }
        }

        public PlaylistViewModel()
        {
        }

        public PlaylistViewModel(IPlaylist source)
        {
            Source = source;
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.CurrentSongPositionChanged += OnCurrentSongPositionChanged;
            playlist.LoopChanged += OnLoopChanged;

            Subscribe(playlist.CurrentSong);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.CurrentSongPositionChanged -= OnCurrentSongPositionChanged;
            playlist.LoopChanged -= OnLoopChanged;

            Unsubscribe(playlist.CurrentSong);
        }

        private void Subscribe(Song song)
        {
            if (song == null) return;

            song.TitleChanged += OnTitleChanged;
            song.ArtistChanged += OnArtistChanged;
            song.DurationChanged += OnDurationChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (song == null) return;

            song.TitleChanged -= OnTitleChanged;
            song.ArtistChanged -= OnArtistChanged;
            song.DurationChanged -= OnDurationChanged;
        }

        private void OnTitleChanged(object sender, SongTitleChangedEventArgs args)
        {
            OnPropertyChanged("CurrentSongTitle");
        }

        private void OnArtistChanged(object sender, SongArtistChangedEventArgs args)
        {
            OnPropertyChanged("CurrentSongArtist");
        }

        private void OnDurationChanged(object sender, SongDurationChangedEventArgs args)
        {
            OnPropertyChanged("CurrentSongDuration");
            OnPropertyChanged("CurrentSongPosition");
        }

        private void OnCurrentSongChanged(object sender, CurrentSongChangedEventArgs args)
        {
            Unsubscribe(args.OldCurrentSong);
            Subscribe(args.NewCurrentSong);

            UpdateCurrentSong();
        }

        private void OnCurrentSongPositionChanged(object sender, CurrentSongPositionChangedEventArgs args)
        {
            OnPropertyChanged("CurrentSongPosition");
        }

        private void OnLoopChanged(object sender, LoopChangedEventArgs args)
        {
            OnPropertyChanged("Loop");
        }

        private void OnShuffleChanged(object sender, ShuffleChangedEventArgs args)
        {
            OnPropertyChanged("Shuffle");
        }

        private void UpdateCurrentSong()
        {
            OnPropertyChanged("CurrentSong");
            OnPropertyChanged("CurrentSongTitle");
            OnPropertyChanged("CurrentSongArtist");

            OnPropertyChanged("CurrentSongPosition");
            OnPropertyChanged("CurrentSongDuration");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
