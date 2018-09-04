using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using PlayerIcons;
using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic
{
    public class BoolToPlayIcon : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Icons.Current.Play;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return false;
        }
    }

    public class BoolToDetailIcon : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Icons.Current.Detail;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return false;
        }
    }

    public class ShuffleToIcon : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((ShuffleType)value)
            {
                case ShuffleType.Complete:
                    return Icons.Current.ShuffleComplete;

                case ShuffleType.OneTime:
                    return Icons.Current.ShuffleOneTime;

                default:
                    return Icons.Current.ShuffleOff;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (ReferenceEquals(value, Icons.Current.ShuffleComplete)) return ShuffleType.Complete;
            else if (ReferenceEquals(value, Icons.Current.ShuffleOneTime)) return ShuffleType.OneTime;

            return ShuffleType.Off;
        }
    }

    public class LoopToIcon : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((LoopType)value)
            {
                case LoopType.All:
                    return Icons.Current.LoopAll;

                case LoopType.Current:
                    return Icons.Current.LoopCurrent;

                default:
                    return Icons.Current.LoopOff;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (ReferenceEquals(value, Icons.Current.LoopAll)) return LoopType.All;
            else if (ReferenceEquals(value, Icons.Current.LoopCurrent)) return LoopType.Current;

            return LoopType.Off;
        }
    }
}
