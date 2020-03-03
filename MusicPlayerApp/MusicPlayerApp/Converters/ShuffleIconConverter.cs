using PlayerIcons;
using System;
using Windows.UI.Xaml.Data;
using MusicPlayer.Models.Shuffle;

namespace FolderMusic.Converters
{
    class ShuffleIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((ShuffleType)value)
            {
                case ShuffleType.Complete:
                    return Icons.Current.ShuffleComplete;

                case ShuffleType.OneTime:
                    return Icons.Current.ShuffleOneTime;

                case ShuffleType.Path:
                    return Icons.Current.ShufflePath;

                default:
                    return Icons.Current.ShuffleOff;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (ReferenceEquals(value, Icons.Current.ShuffleComplete)) return ShuffleType.Complete;
            if (ReferenceEquals(value, Icons.Current.ShuffleOneTime)) return ShuffleType.OneTime;
            if (ReferenceEquals(value, Icons.Current.ShufflePath)) return ShuffleType.Path;

            return ShuffleType.Off;
        }
    }
}
