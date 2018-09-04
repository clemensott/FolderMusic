using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace PlayerIcons
{
    public class Icons
    {
        private static IconCollection darkIcons = new IconCollection(ElementTheme.Dark), lightIcons = new IconCollection(ElementTheme.Light);

        public static ElementTheme Theme { get; set; }

        private static IconCollection IconCollection
        {
            get
            {
                return Theme == ElementTheme.Light ? lightIcons : darkIcons;
            }
        }

        public static IconElement Pause { get { return new SymbolIcon(Symbol.Pause); } }

        public static IconElement Play { get { return new SymbolIcon(Symbol.Play); } }

        public static BitmapImage PlayImage { get { return IconCollection.Play; } }

        public static BitmapImage LoopCurrent { get { return IconCollection.LoopCurrent; } }

        public static BitmapImage LoopOff { get { return IconCollection.LoopOff; } }

        public static BitmapImage LoopOn { get { return IconCollection.LoopOn; } }

        public static BitmapImage ShuffleComplete { get { return IconCollection.ShuffleComplete; } }

        public static BitmapImage ShuffleOff { get { return IconCollection.ShuffleOff; } }

        public static BitmapImage ShuffleOneTime { get { return IconCollection.ShuffleOneTime; } }

        private Icons() { }

        public static void SetIcons(IconCollection dark, IconCollection light)
        {
            darkIcons = dark;
            light = lightIcons;
        }
    }
}
