using Windows.UI.Xaml.Media.Imaging;
using PlayerIcons;

namespace MusicPlayer.Data.Loop
{
    class LoopAll : ILoop
    {
        private static ILoop instance;

        public static ILoop Instance
        {
            get
            {
                if (instance == null) instance = new LoopAll();

                return instance;
            }
        }

        private LoopAll() { }


        public LoopType GetLoopType()
        {
            return LoopType.All;
        }

        public ILoop GetNext()
        {
            return LoopCurrent.Instance;
        }
    }
}
