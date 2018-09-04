using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class SongsCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int intValue = (int)value;

            return string.Format("{0} Song{1}", intValue, intValue != 1 ? "s" : string.Empty);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return 0;
        }
    }
}
