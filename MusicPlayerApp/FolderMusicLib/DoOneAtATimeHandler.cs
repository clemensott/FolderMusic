using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicPlayer
{
    class DoOneAtATimeHandler
    {
        private bool isDoing, doAgain;
        private object lockObj = new object();

        public TimeSpan WaitBeforeDo { get; set; }

        public TimeSpan WaitAfterDo { get; set; }


        public async Task DoAsync(Func<Task> func)
        {
            lock (lockObj)
            {
                if (isDoing)
                {
                    doAgain = true;
                    return;
                }

                isDoing = true;
            }

            while (true)
            {
                await Task.Delay(WaitBeforeDo);

                lock (lockObj) doAgain = false;

                await func();
                await Task.Delay(WaitAfterDo);

                lock (lockObj)
                {
                    if (doAgain)
                    {
                        isDoing = false;
                        break;
                    }
                }
            }
        }
    }
}
