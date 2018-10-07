using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace FolderMusic
{
    class Utils
    {
        public async static void DoSafe(DispatchedHandler handler)
        {
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess) handler();
            else await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, handler);
        }
    }
}
