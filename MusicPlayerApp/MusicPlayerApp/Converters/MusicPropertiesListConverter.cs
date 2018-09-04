using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Data;

namespace FolderMusic.Converters
{
    class MusicPropertiesListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MusicProperties props = (MusicProperties)value;

            switch (parameter.ToString())
            {
                case "Composers":
                    return props.Composers;

                case "Conductors":
                    return props.Conductors;

                case "Genre":
                    return props.Genre;

                case "Producers":
                    return props.Producers;

                case "Writers":
                    return props.Writers;
            }

            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
