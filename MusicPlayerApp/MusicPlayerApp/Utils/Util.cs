using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace FolderMusic
{
    static class Util
    {
        public static async void DoSafe(DispatchedHandler handler)
        {
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess) handler();
            else await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, handler);
        }

        public static TimeSpan Multiply(this TimeSpan timeSpan, double factor)
        {
            return TimeSpan.FromDays(timeSpan.TotalDays * factor);
        }
    }
}
