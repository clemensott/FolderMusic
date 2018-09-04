namespace MusicPlayer.Data.Loop
{
    class LoopOff : ILoop
    {
        private static ILoop instance;

        public static ILoop Instance
        {
            get
            {
                if (instance == null) instance = new LoopOff();

                return instance;
            }
        }

        private LoopOff() { }

        public LoopType GetLoopType()
        {
            return LoopType.Off;
        }

        public ILoop GetNext()
        {
            return LoopAll.Instance;
        }
    }
}
