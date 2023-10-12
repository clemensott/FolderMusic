﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class IsTrueToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }
}
