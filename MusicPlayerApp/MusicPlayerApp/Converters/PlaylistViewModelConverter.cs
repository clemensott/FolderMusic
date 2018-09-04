using MusicPlayer.Data;
using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class PlaylistViewModelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new PlaylistViewModel((IPlaylist)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((PlaylistViewModel)value).Source;
        }
    }
}
