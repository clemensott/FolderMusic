using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace BackgroundAudio2
{
    public sealed class MyBack : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            System.Diagnostics.Debug.WriteLine("MyBack Run wird ausgeführt");
        }
    }
}
