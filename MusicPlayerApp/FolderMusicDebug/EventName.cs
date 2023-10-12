using System;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace MobileDebug
{
    class EventName : INotifyPropertyChanged
    {
        private bool isChecked;
        private ViewModelDebug parent;

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;

                isChecked = value;

                parent.UpdateAllNamesIsChecked();
                NotifyPropertyChanged("IsChecked");
            }
        }

        public string Name { get; private set; }

        public EventName(ViewModelDebug parent, string name, bool isChecked)
        {
            this.isChecked = isChecked;
            Name = name;

            this.parent = parent;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async void NotifyPropertyChanged(string propertyName)
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
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
                }
            }
            catch { }
        }
    }
}
