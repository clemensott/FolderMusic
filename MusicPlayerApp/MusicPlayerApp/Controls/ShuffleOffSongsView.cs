using System;
using System.Linq;
using MusicPlayer.Models;
using MusicPlayer.Models.Interfaces;

namespace FolderMusic
{
    class ShuffleOffSongsView : SongsView
    {
        protected override void OnSourceChanged(ISongCollection oldSongs, ISongCollection newSongs)
        {
            Unsubscribe(oldSongs);
            Subscribe(newSongs);

            SetItemsSource();
        }

        private void Subscribe(ISongCollection songs)
        {
            if (songs != null) songs.Changed += OnSomethingChanged;
        }

        private void Unsubscribe(ISongCollection songs)
        {
            if (songs != null) songs.Changed -= OnSomethingChanged;
        }

        private void OnSomethingChanged(object sender, System.EventArgs e)
        {
            SetItemsSource();
        }

        private void SetItemsSource()
        {
            SetItemsSource(Source.OrderBy(s => s.Title).ThenBy(s => s.Artist));
        }
    }
}
