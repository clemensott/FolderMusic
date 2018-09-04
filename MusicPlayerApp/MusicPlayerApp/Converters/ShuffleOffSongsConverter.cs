using MusicPlayer.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using System.Collections;
using MusicPlayer.Data.Shuffle;
using MusicPlayer;

namespace FolderMusic.Converters
{
    class ShuffleOffSongsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new ShuffleOffSongsCollection((IPlaylist)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    class ShuffleOffSongsCollection : IEnumerable<Song>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private IShuffleCollection source;

        public ShuffleOffSongsCollection(IPlaylist source)
        {
            this.source = source.GetShuffleOffCollection();
            this.source.CollectionChanged += Source_CollectionChanged;

            Subscribe(this.source);
        }

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        private void Subscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>()) Subscribe(song);
        }

        private void Unsubscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>()) Unsubscribe(song);
        }

        private void Subscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged += OnSongChanged;
            song.TitleChanged += OnSongChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged -= OnSongChanged;
            song.TitleChanged -= OnSongChanged;
        }

        private void OnSongChanged(Song sender, EventArgs args)
        {
            int index = source.IndexOf(sender);

            var removeAction = NotifyCollectionChangedAction.Remove;
            var removeArgs = new NotifyCollectionChangedEventArgs(removeAction, sender, index);
            CollectionChanged?.Invoke(this, removeArgs);

            var addAction = NotifyCollectionChangedAction.Add;
            var addArgs = new NotifyCollectionChangedEventArgs(addAction, sender, index);
            CollectionChanged?.Invoke(this, addArgs);
        }

        public IEnumerator<Song> GetEnumerator()
        {
            return source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return source.GetEnumerator();
        }
    }
}
