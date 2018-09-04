using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class RelativePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string absolutePath = (string)value;

            if (absolutePath == string.Empty) return "\\Music";
            int index = absolutePath.IndexOf("\\Music");

            return absolutePath.Remove(0, index);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
