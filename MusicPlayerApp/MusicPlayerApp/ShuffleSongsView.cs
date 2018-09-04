using FolderMusic.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace FolderMusic
{
    class ShuffleSongsView : SongsView
    {
        private static ShuffleSongsConverter converter = new ShuffleSongsConverter();

        protected override IValueConverter GetConverter()
        {
            return converter;
        }
    }
}
