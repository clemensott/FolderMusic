using System;
using MusicPlayer.Models;
using MusicPlayer.Models.EventArgs;

namespace MusicPlayer.SubscriptionsHandler
{
    public class SongSubscriptionsHandler
    {
        public event EventHandler<SubscriptionsEventArgs<Song, SongArtistChangedEventArgs>> ArtistChanged;
        public event EventHandler<SubscriptionsEventArgs<Song, SongTitleChangedEventArgs>> TitleChanged;
        public event EventHandler<SubscriptionsEventArgs<Song, SongDurationChangedEventArgs>> DurationChanged;
        public event EventHandler<SubscriptionsEventArgs<Song, EventArgs>> SomethingChanged;

        public void Subscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged += OnArtistChanged;
            song.TitleChanged += OnTitleChanged;
            song.DurationChanged += OnDurationChanged;
        }

        public void Unsubscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged += OnArtistChanged;
            song.TitleChanged += OnTitleChanged;
            song.DurationChanged += OnDurationChanged;
        }

        private void OnArtistChanged(object sender, SongArtistChangedEventArgs e)
        {
            ArtistChanged?.Invoke(this, new SubscriptionsEventArgs<Song, SongArtistChangedEventArgs>(sender, e));
            SomethingChanged?.Invoke(this, new SubscriptionsEventArgs<Song, EventArgs>(sender, e));
        }

        private void OnTitleChanged(object sender, SongTitleChangedEventArgs e)
        {
            TitleChanged?.Invoke(this, new SubscriptionsEventArgs<Song, SongTitleChangedEventArgs>(sender, e));
            SomethingChanged?.Invoke(this, new SubscriptionsEventArgs<Song, EventArgs>(sender, e));
        }

        private void OnDurationChanged(object sender, SongDurationChangedEventArgs e)
        {
            DurationChanged?.Invoke(this, new SubscriptionsEventArgs<Song, SongDurationChangedEventArgs>(sender, e));
            SomethingChanged?.Invoke(this, new SubscriptionsEventArgs<Song, EventArgs>(sender, e));
        }
    }
}
