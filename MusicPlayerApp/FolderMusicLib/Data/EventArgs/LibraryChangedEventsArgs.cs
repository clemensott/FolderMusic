using System;

namespace MusicPlayer.Data
{
    public class LibraryChangedEventsArgs : EventArgs
    {
        public ILibrary OldValue { get; private set; }

        public ILibrary NewValue { get; private set; }

        internal LibraryChangedEventsArgs(ILibrary oldValue, ILibrary newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
