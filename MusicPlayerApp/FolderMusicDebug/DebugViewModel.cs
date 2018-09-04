using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;

namespace MobileDebug
{
    class ViewModelDebug : INotifyPropertyChanged
    {
        private static ViewModelDebug instance;

        public static ViewModelDebug GetInstance(IList<object> selectedItems)
        {
            if (instance == null) instance = new ViewModelDebug(selectedItems);
            else instance.selectedItems = selectedItems;

            return instance;
        }

        public const string FilterFilename = "Filter.txt";
        private static readonly string filterFilepath = ApplicationData.Current.LocalFolder.Path + "\\" + FilterFilename;

        private bool allNamesIsChecked, isFinding, isUpadetingAllNames, showForeground, showBackground;
        private IList<object> selectedItems;
        private object[] selectedItemsBackup;

        public bool AllNamesIsChecked
        {
            get { return allNamesIsChecked; }
            set
            {
                if (allNamesIsChecked == value) return;

                isUpadetingAllNames = true;
                allNamesIsChecked = value;

                foreach (EventName name in ShowEventNames) name.IsChecked = value;

                NotifyPropertyChanged("AllNamesIsChecked");

                SaveUncheckedNames();
                isUpadetingAllNames = false;
            }
        }


        public bool ShowBackground
        {
            get { return showBackground; }
            set
            {
                if (value == showBackground) return;

                showBackground = value;
                NotifyPropertyChanged("ShowBackground");
                UpdateFilteredEvents();
            }
        }

        public bool ShowForeground
        {
            get { return showForeground; }
            set
            {
                if (value == showForeground) return;

                showForeground = value;
                NotifyPropertyChanged("ShowForeground");
                UpdateFilteredEvents();
            }
        }

        public bool IsFinding
        {
            get { return isFinding; }
            set
            {
                if (value == isFinding) return;

                isFinding = value;

                NotifyPropertyChanged("IsFinding");
                UpdateFilteredEvents();
            }
        }

        public Event[] Events { get; set; }

        public IEnumerable<Event> FilterEvents
        {
            get { return GetFilterEvents().OrderByDescending(e => e.Time).ThenByDescending(e => e.Count); }
        }

        public EventName[] ShowEventNames { get; private set; }

        private ViewModelDebug(IList<object> selectedItems)
        {
            this.selectedItems = selectedItems;
            selectedItemsBackup = new object[0];

            showBackground = true;
            showForeground = true;
            isFinding = true;

            Events = new Event[0];
            ShowEventNames = new EventName[0];

            Reload();
        }

        private IEnumerable<Event> GetFilterEvents()
        {
            if (!ShowBackground && !ShowForeground) return Enumerable.Empty<Event>();

            IEnumerable<Event> filterer = IsFinding ? Join(Events, ShowEventNames) : Events;

            if (ShowBackground && !ShowForeground) return filterer.Where(e => e.TaskId != Manager.ForegroundId);
            else if (ShowForeground && !ShowBackground) return filterer.Where(e => e.TaskId == Manager.ForegroundId);

            return filterer;
        }

        private IEnumerable<Event> Join(IEnumerable<Event> events, IEnumerable<EventName> eventNames)
        {
            return events.Where(e => eventNames.Any(en => en.IsChecked && en.Name == e.Name));
        }

        public async void Reload()
        {
            await LoadDebugEvents();
            await SetFilter();

            UpdateFilteredEvents();
            UpdateAllNamesIsChecked();
        }

        private async Task LoadDebugEvents()
        {
            try
            {
                string backDataEventsString = await FileIO.ReadTextAsync(await Manager.GetBackDebugDataFile());
                string foreDataEventsString = await FileIO.ReadTextAsync(await Manager.GetForeDebugDataFile());

                Events = Event.GetEvents(backDataEventsString).Concat(Event.GetEvents(foreDataEventsString)).ToArray();
            }
            catch (Exception e)
            {
                await new Windows.UI.Popups.MessageDialog("LoadDebugEventsFail:" + GetExceptionMesageses(e)).ShowAsync();
                Events = new Event[0];
            }
        }

        private string GetExceptionMesageses(Exception e)
        {
            string text = string.Empty;

            while (e != null)
            {
                text += string.Format("\nType: {0}\nMess: {1}", e.GetType().Name, e.Message);
                e = e.InnerException;

            }

            return text;
        }

        private async Task SetFilter()
        {
            try
            {
                IList<string> uncheckedNames = await LoadUncheckedNames();

                ShowEventNames = Events.Select(e => e.Name).Distinct().OrderBy(n => n)
                    .Select(n => new EventName(this, n, !uncheckedNames.Contains(n))).ToArray();
            }
            catch (Exception e)
            {
                await new Windows.UI.Popups.MessageDialog("SetFilter:" + GetExceptionMesageses(e)).ShowAsync();
            }
        }

        private async Task<IList<string>> LoadUncheckedNames()
        {
            try
            {
                string[] eventNames = Events.Select(x => x.Name).ToArray();
                return await PathIO.ReadLinesAsync(filterFilepath);
            }
            catch
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync(FilterFilename);
            }

            return new string[0];
        }

        public void StoreSelectedItems()
        {
            selectedItemsBackup = selectedItems.ToArray();
        }

        public void RestoreSelectedItems()
        {
            selectedItems.Clear();

            foreach (object obj in selectedItemsBackup.Where(i => GetFilterEvents().Any(e => e.ToString() == i.ToString())))
            {
                selectedItems.Add(obj);
            }
        }

        public void UpdateAllNamesIsChecked()
        {
            if (isUpadetingAllNames) return;
            isUpadetingAllNames = true;

            allNamesIsChecked = ShowEventNames.All(x => x.IsChecked);

            NotifyPropertyChanged("AllNamesIsChecked");
            SaveUncheckedNames();

            isUpadetingAllNames = false;
        }

        private async void SaveUncheckedNames()
        {
            var uncheckedNames = ShowEventNames.Where(x => !x.IsChecked);

            try
            {
                await PathIO.WriteLinesAsync(filterFilepath, uncheckedNames.Select(x => x.Name));
            }
            catch
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync(FilterFilename);
            }
        }

        private void UpdateFilteredEvents()
        {
            StoreSelectedItems();
            NotifyPropertyChanged("FilterEvents");
            RestoreSelectedItems();
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
