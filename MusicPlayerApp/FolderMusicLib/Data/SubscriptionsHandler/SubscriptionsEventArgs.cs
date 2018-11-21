using System;

namespace MusicPlayer.Data.SubscriptionsHandler
{
    public class SubscriptionsEventArgs<TSource, TEventArgs> : EventArgs where TEventArgs : EventArgs
    {
        public TSource Source { get; private set; }

        public TEventArgs Base { get; private set; }

        public SubscriptionsEventArgs(TSource source, TEventArgs baseArgs)
        {
            Source = source;
            Base = baseArgs;
        }

        public SubscriptionsEventArgs(object source, TEventArgs baseArgs)
        {
            Source = (TSource)source;
            Base = baseArgs;
        }
    }
}
