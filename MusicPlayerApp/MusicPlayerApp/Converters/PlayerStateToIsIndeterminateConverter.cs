using System;
using Windows.Media.Playback;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class PlayerStateToIsIndeterminateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MediaPlayerState state = (MediaPlayerState)value;

            return state != MediaPlayerState.Playing && state != MediaPlayerState.Paused;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
