using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class PlayPauseIconConverter : IValueConverter
    {
        private SymbolIcon pauseIcon, playIcon;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? GetPauseIcon() : GetPlayIcon();
        }

        private SymbolIcon GetPlayIcon()
        {
            try
            {
                if (playIcon == null) playIcon = new SymbolIcon(Symbol.Play);
            }
            catch
            {
                return new SymbolIcon(Symbol.Play);
            }

            return playIcon;
        }

        private SymbolIcon GetPauseIcon()
        {
            try
            {
                if (pauseIcon == null) pauseIcon = new SymbolIcon(Symbol.Pause);
            }
            catch
            {
                return new SymbolIcon(Symbol.Pause);
            }

            return pauseIcon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (SymbolIcon)value == GetPlayIcon();
        }
    }
}
