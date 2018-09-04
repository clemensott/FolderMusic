using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;

namespace FolderMusicDebug
{
    class ViewModel : INotifyPropertyChanged
    {
        public const string DebugDataFilename = "DebugData.txt", FilterFilename = "Filter.txt";
        public static readonly string DebugDataFilepath = ApplicationData.Current.LocalFolder.Path + "\\" + DebugDataFilename;
        private static readonly string filterFilepath = ApplicationData.Current.LocalFolder.Path + "\\" + FilterFilename;

        private static ViewModel instance;

        public static ViewModel Current
        {
            get
            {
                if (instance == null) instance = new ViewModel();

                return instance;
            }
        }

        private bool allNamesIsChecked, doScroll, isUpadetingAllNames;

        public bool AllNamesIsChecked
        {
            get { return allNamesIsChecked; }
            set
            {
                if (allNamesIsChecked == value) return;

                isUpadetingAllNames = true;
                allNamesIsChecked = value;

                foreach (NameCollection name in Names) name.IsChecked = value;

                NotifyPropertyChanged("AllNamesIsChecked");

                SaveUncheckedNames();
                isUpadetingAllNames = false;
            }
        }

        public List<DebugEvent> Events { get; set; }

        public List<DebugEvent> FilterEvents
        {
            get { return Events != null ? Events.Where(x => x.IsChecked).OrderBy(x => x.Time * -1).ToList() : new List<DebugEvent>(); }
        }

        public List<NameCollection> Names { get; private set; }

        private ViewModel()
        {
            Reload();
        }

        public async void Reload()
        {
            doScroll = true;

            await LoadDebugEvents();
            await SetFilter();

            NotifyPropertyChanged("FilterEvents");

            UpdateAllNamesIsChecked();
        }

        private async Task LoadDebugEvents()
        {
            try
            {
                string xmlText = await PathIO.ReadTextAsync(DebugDataFilepath);

                Events = XmlConverter.Deserialize<SaveTextClass>(xmlText).Events.OrderBy(x => x.Time).ToList();
            }
            catch
            {
                Events = new List<DebugEvent>();
            }
        }

        private async Task SetFilter()
        {
            string[] uncheckedNames;

            uncheckedNames = await LoadUncheckedNames();
            Names = new List<NameCollection>();

            foreach (DebugEvent debugEvent in Events)
            {
                int index = Names.FindIndex(x => x.Name == debugEvent.Name);

                if (uncheckedNames.Contains(debugEvent.Name)) debugEvent.IsChecked = false;

                if (index == -1) Names.Add(new NameCollection(debugEvent));
                else Names[index].Add(debugEvent);
            }

            Names = Names.OrderBy(x => x.Name).ToList();
        }

        private async Task<string[]> LoadUncheckedNames()
        {
            try
            {
                string[] eventNames = Events.Select(x => x.Name).ToArray();
                IList<string> uncheckedFilterNames = await PathIO.ReadLinesAsync(filterFilepath).AsTask();

                return uncheckedFilterNames.Where(x => eventNames.Contains(x)).ToArray();
            }
            catch
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync(FilterFilename);
            }

            return new string[0];
        }

        public void UpdateAllNamesIsChecked()
        {
            if (isUpadetingAllNames) return;
            isUpadetingAllNames = true;

            allNamesIsChecked = Names.TrueForAll(x => x.IsChecked);

            NotifyPropertyChanged("AllNamesIsChecked");

            SaveUncheckedNames();

            isUpadetingAllNames = false;
        }

        private async void SaveUncheckedNames()
        {
            var uncheckedNames = Names.Where(x => !x.IsChecked);

            try
            {
                await PathIO.WriteLinesAsync(filterFilepath, uncheckedNames.Select(x => x.Name));
            }
            catch
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync(FilterFilename);
            }
        }

        public bool IsScrollAndSetFalse()
        {
            if (!doScroll) return false;

            doScroll = false;

            return true;
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
