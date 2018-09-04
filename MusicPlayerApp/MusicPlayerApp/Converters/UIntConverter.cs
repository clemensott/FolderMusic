using System;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class UIntConverter : IValueConverter
    {
        private string text;

        public uint CurrentValue { get; set; }

        public UIntConverter()
        {
            CurrentValue = 0;
            text = CurrentValue.ToString();
        }

        public string Convert(object value)
        {
            return Convert((uint)value);
        }

        public string Convert(uint value)
        {
            if (CurrentValue == value) return text;

            CurrentValue = value;

            return text = value.ToString();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Convert(value);
        }

        public uint ConvertBack(object value)
        {
            return ConvertBack(value.ToString());
        }

        public uint ConvertBack(string value)
        {
            uint newValue;
            text = value;

            if (uint.TryParse(text, out newValue)) return CurrentValue = newValue;

            return CurrentValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ConvertBack(value);
        }
    }
}
