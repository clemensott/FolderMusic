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

namespace FolderMusic.Converters
{
    class PlaylistsUpdateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new PlaylistsUpdateCollection((IPlaylistCollection)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    class PlaylistsUpdateCollection : IEnumerable<IPlaylist>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private IPlaylistCollection source;

        public PlaylistsUpdateCollection(IPlaylistCollection source)
        {
            this.source = source;
            source.CollectionChanged += Source_CollectionChanged;

            Subscribe(source);
        }

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        private void Subscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>()) Subscribe(playlist);
        }

        private void Unsubscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>()) Unsubscribe(playlist);
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.Songs.CollectionChanged += OnPlaylistChanged;
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.Songs.CollectionChanged -= OnPlaylistChanged;
        }

        private void OnPlaylistChanged(ISongCollection sender, EventArgs args)
        {
            int index = source.IndexOf(sender.Parent);

            var removeAction = NotifyCollectionChangedAction.Remove;
            var removeArgs = new NotifyCollectionChangedEventArgs(removeAction, sender.Parent, index);
            CollectionChanged?.Invoke(this, removeArgs);

            var addAction = NotifyCollectionChangedAction.Add;
            var addArgs = new NotifyCollectionChangedEventArgs(addAction, sender.Parent, index);
            CollectionChanged?.Invoke(this, addArgs);
        }

        public IEnumerator<IPlaylist> GetEnumerator()
        {
            return source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return source.GetEnumerator();
        }
    }
}
