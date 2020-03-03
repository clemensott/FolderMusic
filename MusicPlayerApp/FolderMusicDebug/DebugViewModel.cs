using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;

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

        public const string FilterFileName = "Filter.txt";
        private static readonly string filterFilepath = ApplicationData.Current.LocalFolder.Path + "\\" + FilterFileName;

        private bool allNamesIsChecked, isFinding, isUpadetingAllNames, showForeground, showBackground, isLoading, forceLog;
        private IList<object> selectedItems;
        private Event[] selectedItemsBackup;
        private string loadingLog;

        public bool IsLoading
        {
            get { return isLoading; }
            private set
            {
                if (value == isLoading) return;
                
                isLoading = value;
                NotifyPropertyChanged("IsLoading");
                NotifyPropertyChanged("ShowLog");
            }
        }

        public bool ForceLog
        {
            get { return forceLog; }
            set
            {
                if (value == forceLog) return;

                forceLog = value;
                NotifyPropertyChanged("ForceLog");
                NotifyPropertyChanged("ShowLog");
            }
        }

        public bool ShowLog => IsLoading || ForceLog;

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

        public string LoadingLog
        {
            get { return loadingLog; }
            set
            {
                if (value == loadingLog) return;

                loadingLog = value;
                NotifyPropertyChanged("LoadingLog");
            }
        }

        private ViewModelDebug(IList<object> selectedItems)
        {
            this.selectedItems = selectedItems;
            selectedItemsBackup = new Event[0];

            showBackground = true;
            showForeground = true;
            isFinding = true;

            Events = new Event[0];
            ShowEventNames = new EventName[0];

            ForceLog = false;

            Reload();
        }

        private IEnumerable<Event> GetFilterEvents()
        {
            if (!ShowBackground && !ShowForeground) return Enumerable.Empty<Event>();

            IEnumerable<Event> filterer = IsFinding ? Join(Events, ShowEventNames) : Events;

            if (ShowBackground && !ShowForeground) return filterer.Where(e => e.BackgroundTaskId != Service.ForegroundId);
            else if (ShowForeground && !ShowBackground) return filterer.Where(e => e.BackgroundTaskId == Service.ForegroundId);

            return filterer;
        }

        private IEnumerable<Event> Join(IEnumerable<Event> events, IEnumerable<EventName> eventNames)
        {
            return events.Where(e => eventNames.Any(en => en.IsChecked && en.Name == e.Name));
        }

        public async void Reload()
        {
            StartLoadingLog("StartLoading");

            IsLoading = true;
            await LoadDebugEvents();
            await SetFilter();

            UpdateFilteredEvents();
            UpdateAllNamesIsChecked();

            IsLoading = false;
            if (Events.Length == 0) ForceLog = true;

            AppandLoadingLog("Relaoding is done: " + GetFilterEvents().Count());
        }

        private async Task LoadDebugEvents()
        {
            try
            {
                AppandLoadingLog("\nGetBackFile: ");
                StorageFile backFile = await Service.GetBackDebugDataFile();
                AppandLoadingLog(backFile.Path);

                AppandLoadingLog("\nGetForeFile: ");
                StorageFile foreFile = await Service.GetForeDebugDataFile();
                AppandLoadingLog(foreFile.Path);

                AppandLoadingLog("\nLoadBackFile: ");
                string backDataEventsString = await FileIO.ReadTextAsync(backFile);
                AppandLoadingLog(backDataEventsString.Length);

                AppandLoadingLog("\nLoadForeFile: ");
                string foreDataEventsString = await FileIO.ReadTextAsync(foreFile);
                AppandLoadingLog(foreDataEventsString.Length);

                AppandLoadingLog("\nGetBackEvents: ");
                Event[] backEvents = Event.GetEvents(backDataEventsString).ToArray();
                AppandLoadingLog(backEvents.Length);

                AppandLoadingLog("\nGetForeEvents: ");
                Event[] foreEvents = Event.GetEvents(foreDataEventsString).ToArray();
                AppandLoadingLog(foreEvents.Length);

                AppandLoadingLog("\nConcatEvents: ");
                Events = backEvents.Concat(foreEvents).ToArray();
                AppandLoadingLog(Events.Length);
            }
            catch (Exception e)
            {
                AppandLoadingLog(GetExceptionMesageses(e));

                await new MessageDialog("LoadDebugEventsFail:" + GetExceptionMesageses(e)).ShowAsync();
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
                await new MessageDialog("SetFilter:" + GetExceptionMesageses(e)).ShowAsync();
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
                await ApplicationData.Current.LocalFolder.CreateFileAsync(FilterFileName);
            }

            return new string[0];
        }

        public void StoreSelectedItems()
        {
            selectedItemsBackup = selectedItems.OfType<Event>().ToArray();
        }

        public void RestoreSelectedItems()
        {
            selectedItems.Clear();

            foreach (object obj in selectedItemsBackup.Where(i => GetFilterEvents().Contains(i)))
            {
                selectedItems.Add(obj);
            }
        }

        public void UpdateAllNamesIsChecked()
        {
            AppandLoadingLog("\nUpdateAllNamesIsChecked: " + isUpadetingAllNames);

            if (isUpadetingAllNames) return;
            isUpadetingAllNames = true;

            AppandLoadingLog("\nAllNamesIsChecked: ");
            allNamesIsChecked = ShowEventNames.All(x => x.IsChecked);
            AppandLoadingLog(allNamesIsChecked);

            NotifyPropertyChanged("AllNamesIsChecked");
            SaveUncheckedNames();

            isUpadetingAllNames = false;
        }

        private async void SaveUncheckedNames()
        {
            AppandLoadingLog("\nUncheckedNames: ");
            IEnumerable<string> uncheckedNames = ShowEventNames.Where(x => !x.IsChecked).Select(x => x.Name);
            AppandLoadingLog(uncheckedNames.Count());

            try
            {
                AppandLoadingLog("\nSaveUncheckedNames...");
                await PathIO.WriteLinesAsync(filterFilepath, uncheckedNames);
                AppandLoadingLog("Done");
            }
            catch (FileNotFoundException)
            {
                try
                {
                    AppandLoadingLog("\nFileNotFound. CreateFile...");
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(FilterFileName);
                    AppandLoadingLog("Done\nTrySaveAgain...");
                    await PathIO.WriteLinesAsync(filterFilepath, uncheckedNames);
                    AppandLoadingLog("Done");
                }
                catch (Exception e)
                {
                    AppandLoadingLog(GetExceptionMesageses(e));
                }
            }
            catch (Exception e)
            {
                AppandLoadingLog(GetExceptionMesageses(e));
            }
        }

        private void StartLoadingLog(object start)
        {
            LoadingLog = start.ToString();
        }

        private void AppandLoadingLog(object log)
        {
            LoadingLog += log.ToString();
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
