using FolderMusicUwpLib;
using Windows.UI.Xaml.Media.Imaging;

namespace LibraryLib
{
    class LoopOff : ILoop
    {
        public BitmapImage GetIcon()
        {
            try
            {
                return Icons.LoopOff;
            }
            catch { }

            return new BitmapImage();
        }

        public LoopKind GetKind()
        {
            return LoopKind.Off;
        }

        public ILoop GetNext()
        {
            return new LoopAll();
        }
    }
}
