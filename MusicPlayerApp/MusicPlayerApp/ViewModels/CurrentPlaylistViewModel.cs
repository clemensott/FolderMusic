using MusicPlayer.Data;
using MusicPlayer.Data.SubscriptionsHandler;
using System;
using System.ComponentModel;

namespace FolderMusic.ViewModels
{
    public class CurrentPlaylistViewModel : INotifyPropertyChanged
    {
        private ILibrary library;
        private LibrarySubscriptionsHandler lsh;

        public string Name { get { return library?.CurrentPlaylist?.Name ?? "Null"; } }

        public Song CurrentSong
        {
            get { return library?.CurrentPlaylist?.CurrentSong; }
            set
            {
                if (library?.CurrentPlaylist == null || value == CurrentSong) return;

                library.CurrentPlaylist.CurrentSong = value;
            }
        }

        public ISongCollection Songs { get { return library?.CurrentPlaylist?.Songs; } }

        public CurrentPlaylistViewModel(ILibrary library)
        {
            this.library = library;
            lsh = LibrarySubscriptionsHandler.GetInstance(library);

            lsh.Loaded += OnLoaded;
            lsh.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            lsh.CurrentPlaylist.CurrentSongChanged += OnCurrentSongChanged;
            lsh.CurrentPlaylist.SongsPropertyChanged += OnSongsPropertyChanged;
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(CurrentSong));
            OnPropertyChanged(nameof(Songs));
        }

        private void OnCurrentPlaylistChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(CurrentSong));
            OnPropertyChanged(nameof(Songs));
        }

        private void OnCurrentSongChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CurrentSong));
        }

        private void OnSongsPropertyChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Songs));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
