using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace FolderMusicUwpLib
{
    public class IconCollection
    {
        private ElementTheme theme;
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

        public IconCollection(ElementTheme theme)
        {
            this.theme = theme;
        }

        private BitmapImage Get(ref BitmapImage image, string part)
        {
            if (image == null) image = GetBitmapImageFromPart(part);

            return image;
        }

        private string GetStartPath(ElementTheme theme)
        {
            return theme == ElementTheme.Light ? "ms-appx:///Assets/Light/" : "ms-appx:///Assets/Dark/";
        }

        private BitmapImage GetBitmapImageFromPart(string part)
        {
            string path = GetStartPath(theme) + part + ".png";
            return new BitmapImage(new Uri(path));
        }
    }
}
