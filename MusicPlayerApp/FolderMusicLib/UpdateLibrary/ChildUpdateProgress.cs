namespace MusicPlayer.UpdateLibrary
{
    public class ChildUpdateProgress : BaseUpdateProgress
    {
        public ChildUpdateProgress(CancelOperationToken token) : base(token)
        {
        }

        public void Increase()
        {
            CurrentCount++;
        }
    }
}
