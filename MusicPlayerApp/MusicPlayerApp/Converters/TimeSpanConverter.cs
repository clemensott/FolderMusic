using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class TimeSpanConverter : IValueConverter
    {
        public static string Convert(TimeSpan ts)
        {
            return ts.Hours > 0 ? ts.ToString("hh\\:mm\\:ss") : ts.ToString("mm\\:ss");
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Convert((TimeSpan)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
