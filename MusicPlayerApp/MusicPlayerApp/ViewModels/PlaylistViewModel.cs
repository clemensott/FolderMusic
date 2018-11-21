using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using MusicPlayer.Data.SubscriptionsHandler;
using System;
using System.ComponentModel;

namespace FolderMusic.ViewModels
{
    public class PlaylistViewModel : INotifyPropertyChanged
    {
        private PlaylistSubscriptionsHandler psh;

        public IPlaylist Base { get; private set; }

        public string Name { get { return Base?.Name ?? "Null"; } }

        public string AbsolutePath { get { return Base?.AbsolutePath ?? "Null"; } }

        public Song CurrentSong
        {
            get { return Base?.CurrentSong; }
            set { if (Base != null) Base.CurrentSong = value; }
        }

        public ISongCollection Songs
        {
            get { return Base?.Songs; }
            set { if (Base != null) Base.Songs = value; }
        }

        public int SongsCount { get { return Base?.Songs?.Count ?? 0; } }

        public LoopType Loop
        {
            get { return Base?.Loop ?? LoopType.Off; }
            set { if (Base != null) Base.Loop = value; }
        }

        public ShuffleType Shuffle
        {
            get { return Songs?.Shuffle?.Type ?? ShuffleType.Off; }
            set { if (Songs?.Shuffle != null && value != Songs.Shuffle.Type) Songs.SetShuffleType(value); }
        }

        public PlaylistViewModel(IPlaylist playlist)
        {
            this.Base = playlist;
            psh = new PlaylistSubscriptionsHandler();

            psh.CurrentSongChanged += OnCurrentSongChanged;
            psh.LoopChanged += OnLoopChanged;
            psh.ShuffleChanged += OnShuffleChanged;
            psh.SongsPropertyChanged += OnSongsPropertyChanged;
            psh.SongCollectionChanged += OnSongCollectionChanged;

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

        private void OnShuffleChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Shuffle));
        }

        private void OnSongsPropertyChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Songs));
            OnPropertyChanged(nameof(SongsCount));
            OnPropertyChanged(nameof(Shuffle));
        }

        private void OnSongCollectionChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(SongsCount));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
