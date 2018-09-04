using MusicPlayer.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.UI.Xaml.Data;
using System.Collections;

namespace FolderMusic.Converters
{
    class ShuffleSongsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MobileDebug.Manager.WriteEvent("ShuffleSongsCon", value);
            return new ShuffleSongsCollection((IPlaylist)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    class ShuffleSongsCollection : IEnumerable<Song>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private IPlaylist source;

        public ShuffleSongsCollection(IPlaylist source)
        {
            this.source = source;
            source.ShuffleChanged += Source_ShuffleChanged;
            source.ShuffleSongs.CollectionChanged += Shuffle_CollectionChanged;

            Subscribe(source.ShuffleSongs);
        }

        private void Source_ShuffleChanged(IPlaylist sender, ShuffleChangedEventArgs args)
        {
            var resetAction = NotifyCollectionChangedAction.Reset;
            var resetArgs = new NotifyCollectionChangedEventArgs(resetAction);
            CollectionChanged?.Invoke(this, resetArgs);
        }

        private void Shuffle_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
            int index = source.ShuffleSongs.IndexOf(sender);

            var removeAction = NotifyCollectionChangedAction.Remove;
            var removeArgs = new NotifyCollectionChangedEventArgs(removeAction, sender, index);
            CollectionChanged?.Invoke(this, removeArgs);

            var addAction = NotifyCollectionChangedAction.Add;
            var addArgs = new NotifyCollectionChangedEventArgs(addAction, sender, index);
            CollectionChanged?.Invoke(this, addArgs);
        }

        public IEnumerator<Song> GetEnumerator()
        {
            return source.ShuffleSongs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return source.ShuffleSongs.GetEnumerator();
        }
    }
}
