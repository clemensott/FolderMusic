using FolderMusicUwpLib;
using Windows.UI.Xaml.Media.Imaging;

namespace LibraryLib
{
    class LoopCurrent : ILoop
    {
        public BitmapImage GetIcon()
        {
            try
            {
                return Icons.LoopCurrent;
            }
            catch { }

            return new BitmapImage();
        }

        public LoopKind GetKind()
        {
            return LoopKind.Current;
        }

        public ILoop GetNext()
        {
            return new LoopOff();
        }
    }
}
