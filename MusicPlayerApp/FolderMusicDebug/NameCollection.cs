using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace FolderMusicDebug
{
    class NameCollection : List<DebugEvent>, INotifyPropertyChanged
    {
        private bool isChecked;

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;

                isChecked = value;

                foreach (DebugEvent debugEvent in this) debugEvent.IsChecked = value;

                ViewModelDebug.Current.UpdateAllNamesIsChecked();
                NotifyPropertyChanged("IsChecked");
            }
        }

        public string Name { get; private set; }

        public NameCollection(DebugEvent debugEvent) : base()
        {
            isChecked = debugEvent.IsChecked;
            Name = debugEvent.Name;

            Add(debugEvent);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                if (null == PropertyChanged) return;

                if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
                }
            }
            catch { }
        }
    }
}
