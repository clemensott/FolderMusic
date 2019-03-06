using System;
using MusicPlayer.Data;
using System.Linq;

namespace FolderMusic
{
    class ShuffleOffSongsView : SongsView
    {
        protected override void OnSouceChanged(ISongCollection oldSongs, ISongCollection newSongs)
        {
            Unsubscribe(oldSongs);
            Subscribe(newSongs);

            SetItemsSource();
        }

        private void Subscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.ShuffleChanged += OnSomethingChanged;

            foreach (Song song in songs) Subscribe(song);
        }

        private void Unsubscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.ShuffleChanged -= OnSomethingChanged;

            foreach (Song song in songs) Unsubscribe(song);
        }

        private void Subscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged += OnSomethingChanged;
            song.TitleChanged += OnSomethingChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged -= OnSomethingChanged;
            song.TitleChanged -= OnSomethingChanged;
        }

        private void OnSomethingChanged(object sender, EventArgs e)
        {
            SetItemsSource();
        }

        private void SetItemsSource()
        {
            SetItemsSource(Source.OrderBy(s => s.Title).ThenBy(s => s.Artist));
        }
    }
}
