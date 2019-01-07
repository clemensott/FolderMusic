using System;

namespace MusicPlayer
{
    public class StopOperationToken
    {
        private bool isStopped;

        public event EventHandler Stopped;

        public bool IsStopped
        {
            get { return isStopped; }
            private set
            {
                if (value == isStopped) return;

                isStopped = value;
                Stopped?.Invoke(this, EventArgs.Empty);
            }
        }

        public StopOperationToken()
        {
            isStopped = false;
        }

        public void Stop()
        {
            IsStopped = true;
        }
    }
}
