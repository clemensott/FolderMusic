using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class MillisToTimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return TimeSpan.FromMilliseconds((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((TimeSpan)value).TotalMilliseconds;
        }
    }
}
