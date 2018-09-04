using PlayerIcons;
using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class PlayIconConverter : IValueConverter
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
}
