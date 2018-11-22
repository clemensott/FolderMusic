using MusicPlayer.Data;
using MusicPlayer.Data.SubscriptionsHandler;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.Media.Playback;

namespace FolderMusic.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private LibrarySubscriptionsHandler lsh;

        public bool IsPlaying { get { return Library.IsPlaying; } }

        public ILibrary Library { get; private set; }

        public CurrentSongViewModel CurrentSong { get; private set; }

        public CurrentPlaylistViewModel CurrentPlaylist { get; private set; }

        public PlaylistViewModel SelectedPlaylist
        {
            get { return Playlists.FirstOrDefault(p => p.Base == Library.CurrentPlaylist); }
            set { Library.CurrentPlaylist = value.Base; }
        }

        public ObservableCollection<PlaylistViewModel> Playlists { get; private set; }

        public MediaPlayer BackgroundPlayer { get { return BackgroundMediaPlayer.Current; } }

        public MainViewModel(ILibrary library)
        {
            Library = library;
            CurrentSong = new CurrentSongViewModel(library);
            CurrentPlaylist = new CurrentPlaylistViewModel(library);
            Playlists = new ObservableCollection<PlaylistViewModel>(Library.Playlists.Select(p => new PlaylistViewModel(p)));

            lsh = LibrarySubscriptionsHandler.GetInstance(library);

            lsh.Loaded += OnLoaded;
            lsh.PlayStateChanged += OnPlayStateChanged;
            lsh.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            lsh.PlaylistsPropertyChanged += OnPlaylistsPropertyChanged;
            lsh.PlaylistCollectionChanged += OnPlaylistCollectionChanged;
        }

        private void OnLoaded(object sender, SubscriptionsEventArgs<ILibrary, EventArgs> e)
        {
            foreach (PlaylistViewModel playlist in Playlists.Where(p => !e.Source.Playlists.Contains(p.Base)).ToArray())
            {
                Playlists.Remove(playlist);
            }

            int i = 0;
            foreach (IPlaylist playlist in e.Source.Playlists)
            {
                if (!Playlists.Any(p => p.Base == playlist)) Playlists.Insert(i, new PlaylistViewModel(playlist));

                i++;
            }
        }

        private void OnPlayStateChanged(object sender, SubscriptionsEventArgs<ILibrary, PlayStateChangedEventArgs> e)
        {
            NotifyPropertyChanged(nameof(IsPlaying));
        }

        private void OnCurrentPlaylistChanged(object sender, SubscriptionsEventArgs<ILibrary, CurrentPlaylistChangedEventArgs> e)
        {
            NotifyPropertyChanged(nameof(SelectedPlaylist));
        }

        private void OnPlaylistsPropertyChanged(object sender, SubscriptionsEventArgs<ILibrary, PlaylistsChangedEventArgs> e)
        {
            Playlists.Clear();

            foreach (IPlaylist playlist in e.Base.NewPlaylists)
            {
                Playlists.Add(new PlaylistViewModel(playlist));
            }
        }

        private void OnPlaylistCollectionChanged(object sender, SubscriptionsEventArgs<IPlaylistCollection, PlaylistCollectionChangedEventArgs> e)
        {
            foreach (ChangeCollectionItem<IPlaylist> change in e.Base.RemovedPlaylists)
            {
                Playlists.RemoveAt(change.Index);
            }

            foreach (ChangeCollectionItem<IPlaylist> change in e.Base.AddedPlaylists)
            {
                Playlists.Insert(change.Index, new PlaylistViewModel(change.Item));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                if (null == PropertyChanged) return;

                Utils.DoSafe(() => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("ViewModelNotifyFail", e);
            }
        }
    }
}
