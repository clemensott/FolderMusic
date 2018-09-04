using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class PlayPauseTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? "Pause" : "Play";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (string)value == "Pause";
        }
    }
}
