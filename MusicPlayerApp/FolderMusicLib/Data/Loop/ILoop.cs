using Windows.UI.Xaml.Media.Imaging;

namespace MusicPlayer.Data.Loop
{
    public enum LoopType { Off, All, Current };

    interface ILoop
    {
        ILoop GetNext();

        LoopType GetLoopType();
    }
}
