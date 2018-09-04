using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class SubstractConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ConvertToDouble(value) - ConvertToDouble(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ConvertToDouble(value) + ConvertToDouble(parameter);
        }

        private double ConvertToDouble(object value)
        {
            try
            {
                return System.Convert.ToDouble(value);
            }
            catch
            {
                return 0;
            }
        }
    }
}
