using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using MusicPlayer.Data.SubscriptionsHandler;
using System;
using System.ComponentModel;

namespace FolderMusic.ViewModels
{
    class PlaylistViewModel : INotifyPropertyChanged
    {
        private IPlaylist playlist;
        private PlaylistSubscriptionsHandler psh;

        public string Name { get { return playlist?.Name ?? "Null"; } }

        public string AbsolutePath { get { return playlist?.AbsolutePath ?? "Null"; } }

        public Song CurrentSong
        {
            get { return playlist?.CurrentSong; }
            set { if (playlist != null) playlist.CurrentSong = value; }
        }

        public ISongCollection Songs
        {
            get { return playlist?.Songs; }
            set { if (playlist != null) playlist.Songs = value; }
        }

        public LoopType Loop
        {
            get { return playlist?.Loop ?? LoopType.Off; }
            set { if (playlist != null) playlist.Loop = value; }
        }

        public ShuffleType Shuffle
        {
            get { return Songs?.Shuffle?.Type ?? ShuffleType.Off; }
            set { if (Songs?.Shuffle != null && value != Songs.Shuffle.Type) Songs.SetShuffleType(value); }
        }

        public PlaylistViewModel(IPlaylist playlist)
        {
            this.playlist = playlist;
            psh = new PlaylistSubscriptionsHandler();

            psh.CurrentSongChanged += OnCurrentSongChanged;
            psh.LoopChanged += OnLoopChanged;
            psh.ShuffleChanged += OnShuffleChanged;
            psh.SongsPropertyChanged += OnSongsPropertyChanged;

            psh.Subscribe(playlist);
        }

        private void OnCurrentSongChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CurrentSong));
        }

        private void OnLoopChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Loop));
        }

        private void OnSongsPropertyChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Songs));
            OnPropertyChanged(nameof(Shuffle));
        }

        private void OnShuffleChanged(object sender, EventArgs e)
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
