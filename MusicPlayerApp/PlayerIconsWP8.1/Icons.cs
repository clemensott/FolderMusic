using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace PlayerIcons
{
    public abstract class Icons
    {
        private static Icons lightIcons, darkIcons;

        public static Icons Current
        {
            get
            {
                if (IsLightTheme())
                {
                    if (lightIcons == null) lightIcons = new LightIcons();

                    return lightIcons;
                }
                else
                {
                    if (darkIcons == null) darkIcons = new DarkIcons();

                    return darkIcons;
                }
            }
        }

        private static bool IsLightTheme()
        {
            try
            {
                //Color color = (Color)Application.Current.Resources["PhoneBackgroundColor"];
                return (Color)Application.Current.Resources["PhoneBackgroundColor"] == Colors.White;
            }
            catch { }

            return false;
        }

        private BitmapImage play;
        private BitmapImage loopCurrent, loopOff, loopOn;
        private BitmapImage shuffleComplete, shuffleOff, shuffleOneTime;
        private BitmapImage detail;

        public BitmapImage Play { get { return Get(ref play, "PlayButton"); } }

        public BitmapImage LoopCurrent { get { return Get(ref loopCurrent, "loopCurrent"); } }

        public BitmapImage LoopOff { get { return Get(ref loopOff, "loopOff"); } }

        public BitmapImage LoopAll { get { return Get(ref loopOn, "loopAll"); } }

        public BitmapImage ShuffleComplete { get { return Get(ref shuffleComplete, "shuffleComplete"); } }

        public BitmapImage ShuffleOff { get { return Get(ref shuffleOff, "shuffleOff"); } }

        public BitmapImage ShuffleOneTime { get { return Get(ref shuffleOneTime, "shuffleOn"); } }

        public BitmapImage Detail { get { return Get(ref detail, "DetailButton"); } }

        private BitmapImage Get(ref BitmapImage image, string part)
        {
            if (image == null) image = GetBitmapImageFromPart(part);

            return image;
        }

        private BitmapImage GetBitmapImageFromPart(string part)
        {
            string path = GetStartPath() + part + ".png";
            return new BitmapImage(new Uri(path));
        }

        protected abstract string GetStartPath();
    }
}
