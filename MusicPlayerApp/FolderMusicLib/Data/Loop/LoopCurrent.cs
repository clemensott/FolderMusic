namespace MusicPlayer.Data.Loop
{
    class LoopCurrent : ILoop
    {
        private static ILoop instance;

        public static ILoop Instance
        {
            get
            {
                if (instance == null) instance = new LoopCurrent();

                return instance;
            }
        }

        private LoopCurrent() { }


        public LoopType GetLoopType()
        {
            return LoopType.Current;
        }

        public ILoop GetNext()
        {
            return LoopOff.Instance;
        }
    }
}
