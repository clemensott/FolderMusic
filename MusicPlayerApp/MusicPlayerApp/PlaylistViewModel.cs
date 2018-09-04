using MusicPlayer.Data;
using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using System.ComponentModel;

namespace FolderMusic
{
    class PlaylistViewModel : INotifyPropertyChanged
    {
        private IPlaylist source;

        public double CurrentSongPositionPercent
        {
            get { return Source?.CurrentSongPositionPercent ?? 0; }
            set { if (Source != null) Source.CurrentSongPositionPercent = value; }
        }

        public double CurrentSongPosition
        {
            get { return CurrentSongPositionPercent * CurrentSongDuration; }
            set
            {
                if (Source == null || CurrentSongDuration == 0) return;

                Source.CurrentSongPositionPercent = value / CurrentSongDuration;
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

        public ShuffleType Shuffle
        {
            get { return Source?.Shuffle ?? ShuffleType.Off; }
            set { if (Source != null) Source.Shuffle = value; }
        }

        public LoopType Loop
        {
            get { return Source?.Loop ?? LoopType.Off; }
            set { if (Source != null) Source.Loop = value; }
        }

        public ISongCollection Songs { get { return Source?.Songs; } }

        public IShuffleCollection ShuffleSongs { get { return Source?.ShuffleSongs; } }

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
            playlist.ShuffleChanged += OnShuffleChanged;

            Subscribe(playlist.CurrentSong);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.CurrentSongPositionChanged -= OnCurrentSongPositionChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.ShuffleChanged -= OnShuffleChanged;

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

        private void OnTitleChanged(Song sender, SongTitleChangedEventArgs args)
        {
            OnPropertyChanged("CurrentSongTitle");
        }

        private void OnArtistChanged(Song sender, SongArtistChangedEventArgs args)
        {
            OnPropertyChanged("CurrentSongArtist");
        }

        private void OnDurationChanged(Song sender, SongDurationChangedEventArgs args)
        {
            OnPropertyChanged("CurrentSongDuration");
            OnPropertyChanged("CurrentSongPosition");
        }

        private void OnCurrentSongChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            Unsubscribe(args.OldCurrentSong);
            Subscribe(args.NewCurrentSong);

            UpdateCurrentSong();
        }

        private void OnCurrentSongPositionChanged(IPlaylist sender, CurrentSongPositionChangedEventArgs args)
        {
            OnPropertyChanged("CurrentSongPosition");
        }

        private void OnLoopChanged(IPlaylist sender, LoopChangedEventArgs args)
        {
            OnPropertyChanged("Loop");
        }

        private void OnShuffleChanged(IPlaylist sender, ShuffleChangedEventArgs args)
        {
            OnPropertyChanged("Shuffle");
            OnPropertyChanged("ShuffleSongs");
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
