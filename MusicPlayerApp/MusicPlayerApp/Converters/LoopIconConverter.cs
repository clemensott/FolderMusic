using PlayerIcons;
using System;
using Windows.UI.Xaml.Data;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.Interfaces;

namespace FolderMusic.Converters
{
    class LoopIconConverter : IValueConverter
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
