using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Models;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;
using MusicPlayer.Models.Shuffle;

namespace MusicPlayer.SubscriptionsHandler
{
    public class PlaylistSubscriptionsHandler
    {
        public event EventHandler<SubscriptionsEventArgs<IPlaylist, CurrentSongChangedEventArgs>> CurrentSongChanged;
        public event EventHandler<SubscriptionsEventArgs<IPlaylist, CurrentSongPositionChangedEventArgs>> CurrentSongPositionChanged;
        public event EventHandler<SubscriptionsEventArgs<IPlaylist, LoopChangedEventArgs>> LoopChanged;
        public event EventHandler<SubscriptionsEventArgs<IPlaylist, SongsChangedEventArgs>> SongsPropertyChanged;
        public event EventHandler<SubscriptionsEventArgs<ISongCollection, SongCollectionChangedEventArgs>> SongCollectionChanged;
        public event EventHandler<SubscriptionsEventArgs<ISongCollection, ShuffleChangedEventArgs>> ShuffleChanged;
        public event EventHandler<SubscriptionsEventArgs<IShuffleCollection, ShuffleCollectionChangedEventArgs>> ShuffleCollectionChanged;

        public SongSubscriptionsHandler AllSongs { get; private set; }

        public SongSubscriptionsHandler CurrentSong { get; private set; }

        public SongSubscriptionsHandler OtherSongs { get; private set; }

        public PlaylistSubscriptionsHandler()
        {
            AllSongs = new SongSubscriptionsHandler();
            CurrentSong = new SongSubscriptionsHandler();
            OtherSongs = new SongSubscriptionsHandler();
        }

        public void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.CurrentSongPositionChanged += OnCurrentSongPositionChanged;
            playlist.LoopChanged += OnLoopChanged;
            playlist.SongsChanged += OnSongsPropertyChanged;

            Subscribe(playlist.Songs);
        }

        public void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.CurrentSongPositionChanged -= OnCurrentSongPositionChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.SongsChanged -= OnSongsPropertyChanged;

            Unsubscribe(playlist.Songs);
        }

        private void Subscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.Changed += OnSongsCollectionChanged;
            songs.ShuffleChanged += OnShufflePropertyChanged;

            Subscribe(songs.AsEnumerable());
            Subscribe(songs.Shuffle);
        }

        private void Unsubscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.Changed -= OnSongsCollectionChanged;
            songs.ShuffleChanged -= OnShufflePropertyChanged;

            Unsubscribe(songs.AsEnumerable());
            Unsubscribe(songs.Shuffle);
        }

        private void Subscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>())
            {
                bool isCurrentSong = song == song.Parent.Parent.CurrentSong;

                if (isCurrentSong) CurrentSong.Subscribe(song);
                else OtherSongs.Subscribe(song);

                AllSongs.Subscribe(song);
            }
        }

        private void Unsubscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>())
            {
                CurrentSong.Unsubscribe(song);
                OtherSongs.Unsubscribe(song);
                AllSongs.Unsubscribe(song);
            }
        }

        private void Subscribe(IShuffleCollection shuffle)
        {
            if (shuffle == null) return;

            shuffle.Changed += OnShuffleCollectionChanged;
        }

        private void Unsubscribe(IShuffleCollection shuffle)
        {
            if (shuffle == null) return;

            shuffle.Changed -= OnShuffleCollectionChanged;
        }

        private void OnCurrentSongChanged(object sender, CurrentSongChangedEventArgs e)
        {
            CurrentSong.Unsubscribe(e.OldCurrentSong);
            CurrentSong.Subscribe(e.NewCurrentSong);

            OtherSongs.Unsubscribe(e.NewCurrentSong);
            OtherSongs.Subscribe(e.OldCurrentSong);

            CurrentSongChanged?.Invoke(this, new SubscriptionsEventArgs<IPlaylist, CurrentSongChangedEventArgs>(sender, e));
        }

        private void OnCurrentSongPositionChanged(object sender, CurrentSongPositionChangedEventArgs e)
        {
            CurrentSongPositionChanged?.Invoke(this, new SubscriptionsEventArgs<IPlaylist, CurrentSongPositionChangedEventArgs>(sender, e));
        }

        private void OnLoopChanged(object sender, LoopChangedEventArgs e)
        {
            LoopChanged?.Invoke(this, new SubscriptionsEventArgs<IPlaylist, LoopChangedEventArgs>(sender, e));
        }

        private void OnSongsPropertyChanged(object sender, SongsChangedEventArgs e)
        {
            Unsubscribe(e.OldSongs);
            Subscribe(e.NewSongs);

            SongsPropertyChanged?.Invoke(this, new SubscriptionsEventArgs<IPlaylist, SongsChangedEventArgs>(sender, e));
        }

        private void OnSongsCollectionChanged(object sender, SongCollectionChangedEventArgs e)
        {
            MobileDebug.Service.WriteEvent("PlaylistSubscribtionHandler.OnSongsCollectionChanged", (sender as ISongCollection)?.Parent?.Name);
            Unsubscribe(e.GetRemoved());
            Subscribe(e.GetAdded());

            SongCollectionChanged?.Invoke(this, new SubscriptionsEventArgs<ISongCollection, SongCollectionChangedEventArgs>(sender, e));
        }

        private void OnShufflePropertyChanged(object sender, ShuffleChangedEventArgs e)
        {
            Unsubscribe(e.OldShuffleSongs);
            Subscribe(e.NewShuffleSongs);

            ShuffleChanged?.Invoke(this, new SubscriptionsEventArgs<ISongCollection, ShuffleChangedEventArgs>(sender, e));
        }

        private void OnShuffleCollectionChanged(object sender, ShuffleCollectionChangedEventArgs e)
        {
            ShuffleCollectionChanged?.Invoke(this, new SubscriptionsEventArgs<IShuffleCollection, ShuffleCollectionChangedEventArgs>(sender, e));
        }
    }
}
