using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

                ViewModel.Current.UpdateAllNamesIsChecked();
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

        public async void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                if (null == PropertyChanged) return;

                await Windows.ApplicationModel.Core.CoreApplication.MainView.
                    CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
            }
            catch { }
        }
    }
}
