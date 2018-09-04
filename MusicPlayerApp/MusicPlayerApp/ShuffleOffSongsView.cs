using FolderMusic.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace FolderMusic
{
    class ShuffleOffSongsView : SongsView
    {
        private static ShuffleOffSongsConverter converter = new ShuffleOffSongsConverter();

        protected override IValueConverter GetConverter()
        {
            return converter;
        }
    }
}
