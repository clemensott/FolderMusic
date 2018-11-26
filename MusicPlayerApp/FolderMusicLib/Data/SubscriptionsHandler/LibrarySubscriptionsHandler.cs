using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data.SubscriptionsHandler
{
    public class LibrarySubscriptionsHandler
    {
        private static Dictionary<ILibrary, LibrarySubscriptionsHandler> instances = new Dictionary<ILibrary, LibrarySubscriptionsHandler>();

        public static LibrarySubscriptionsHandler GetInstance(ILibrary library)
        {
            LibrarySubscriptionsHandler instance;

            if (!instances.TryGetValue(library, out instance))
            {
                instance = new LibrarySubscriptionsHandler();
                instance.Subscribe(library);

                instances.Add(library, instance);
            }

            return instance;
        }

        public event EventHandler<SubscriptionsEventArgs<ILibrary, EventArgs>> Loaded;
        public event EventHandler<SubscriptionsEventArgs<ILibrary, EventArgs>> SettingsChanged;
        public event EventHandler<SubscriptionsEventArgs<ILibrary, PlayStateChangedEventArgs>> PlayStateChanged;
        public event EventHandler<SubscriptionsEventArgs<ILibrary, PlayerStateChangedEventArgs>> PlayerStateChanged;
        public event EventHandler<SubscriptionsEventArgs<ILibrary, CurrentPlaylistChangedEventArgs>> CurrentPlaylistChanged;
        public event EventHandler<SubscriptionsEventArgs<ILibrary, PlaylistsChangedEventArgs>> PlaylistsPropertyChanged;
        public event EventHandler<SubscriptionsEventArgs<IPlaylistCollection, PlaylistCollectionChangedEventArgs>> PlaylistCollectionChanged;
        public event EventHandler<SubscriptionsEventArgs<SkipSongs, EventArgs>> SkippedSong;

        public PlaylistSubscriptionsHandler AllPlaylists { get; private set; }

        public PlaylistSubscriptionsHandler CurrentPlaylist { get; private set; }

        public PlaylistSubscriptionsHandler OtherPlaylists { get; private set; }

        public LibrarySubscriptionsHandler()
        {
            AllPlaylists = new PlaylistSubscriptionsHandler();
            CurrentPlaylist = new PlaylistSubscriptionsHandler();
            OtherPlaylists = new PlaylistSubscriptionsHandler();
        }

        public void Subscribe(ILibrary library)
        {
            if (library == null) return;

            if (!library.IsLoaded) library.Loaded += OnLoaded;
            else
            {
                library.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
                library.PlaylistsChanged += OnPlaylistsPropertyChanged;
                library.PlayStateChanged += OnPlayStateChanged;
                library.PlayerStateChanged += OnPlayerStateChanged;
                library.SettingsChanged += OnSettingsChanged;
                library.SkippedSongs.SkippedSong += OnSkippedSong;

                Subscribe(library.Playlists);
            }
        }

        public void Unsubscribe(ILibrary library)
        {
            if (library == null) return;

            library.Loaded -= OnLoaded;
            library.CurrentPlaylistChanged -= OnCurrentPlaylistChanged;
            library.PlaylistsChanged -= OnPlaylistsPropertyChanged;
            library.PlayStateChanged -= OnPlayStateChanged;
            library.SettingsChanged -= OnSettingsChanged;
            library.SkippedSongs.SkippedSong -= OnSkippedSong;

            Unsubscribe(library.Playlists);
        }

        private void Subscribe(IPlaylistCollection playlists)
        {
            if (playlists == null) return;

            playlists.Changed += OnPlaylistsCollectionChanged;

            Subscribe(playlists.AsEnumerable());
        }

        private void Unsubscribe(IPlaylistCollection playlists)
        {
            if (playlists == null) return;

            playlists.Changed += OnPlaylistsCollectionChanged;

            Unsubscribe(playlists.AsEnumerable());
        }

        private void Subscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                bool isCurrentPlaylist = playlist == playlist.Parent.Parent.CurrentPlaylist;

                if (isCurrentPlaylist) CurrentPlaylist.Subscribe(playlist);
                else OtherPlaylists.Subscribe(playlist);

                AllPlaylists.Subscribe(playlist);
            }
        }

        private void Unsubscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                CurrentPlaylist.Unsubscribe(playlist);
                OtherPlaylists.Unsubscribe(playlist);

                AllPlaylists.Unsubscribe(playlist);
            }
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            Unsubscribe((ILibrary)sender);
            Subscribe((ILibrary)sender);

            Loaded?.Invoke(this, new SubscriptionsEventArgs<ILibrary, EventArgs>(sender, e));
        }

        private void OnCurrentPlaylistChanged(object sender, CurrentPlaylistChangedEventArgs e)
        {
            CurrentPlaylist.Unsubscribe(e.OldCurrentPlaylist);
            CurrentPlaylist.Subscribe(e.NewCurrentPlaylist);

            OtherPlaylists.Unsubscribe(e.NewCurrentPlaylist);
            OtherPlaylists.Subscribe(e.OldCurrentPlaylist);

            CurrentPlaylistChanged?.Invoke(this, new SubscriptionsEventArgs<ILibrary, CurrentPlaylistChangedEventArgs>(sender, e));
        }

        private void OnPlaylistsPropertyChanged(object sender, PlaylistsChangedEventArgs e)
        {
            Unsubscribe(e.OldPlaylists);
            Subscribe(e.NewPlaylists);

            PlaylistsPropertyChanged?.Invoke(this, new SubscriptionsEventArgs<ILibrary, PlaylistsChangedEventArgs>(sender, e));
        }

        private void OnPlayStateChanged(object sender, PlayStateChangedEventArgs e)
        {
            PlayStateChanged?.Invoke(this, new SubscriptionsEventArgs<ILibrary, PlayStateChangedEventArgs>(sender, e));
        }

        private void OnPlayerStateChanged(object sender, PlayerStateChangedEventArgs e)
        {
            PlayerStateChanged?.Invoke(this, new SubscriptionsEventArgs<ILibrary, PlayerStateChangedEventArgs>(sender, e));
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            SettingsChanged?.Invoke(this, new SubscriptionsEventArgs<ILibrary, EventArgs>(sender, e));
        }

        private void OnSkippedSong(object sender, EventArgs e)
        {
            SkippedSong?.Invoke(this, new SubscriptionsEventArgs<SkipSongs, EventArgs>(sender, e));
        }

        private void OnPlaylistsCollectionChanged(object sender, PlaylistCollectionChangedEventArgs e)
        {
            Unsubscribe(e.GetRemoved());
            Subscribe(e.GetAdded());

            PlaylistCollectionChanged?.Invoke(this, new SubscriptionsEventArgs<IPlaylistCollection, PlaylistCollectionChangedEventArgs>(sender, e));
        }
    }
}
