using Windows.UI.Xaml.Media.Imaging;

namespace LibraryLib
{
    interface ILoop
    {
        ILoop GetNext();

        BitmapImage GetIcon();

        LoopKind GetKind();
    }
}
