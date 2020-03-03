using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Storage;

namespace MusicPlayer.Models
{
    public class CurrentPlaySong
    {
        private static CurrentPlaySong instance;

        public static CurrentPlaySong Current
        {
            get
            {
                if (instance == null) instance = new CurrentPlaySong();

                return instance;
            }
        }

        private readonly ApplicationDataContainer container;

        public double Position
        {
            get { return GetValue<double>(nameof(Position)); }
            set { SetValue(nameof(Position), value); }
        }

        public string Title
        {
            get { return GetValue<string>(nameof(Title), string.Empty); }
            set { SetValue(nameof(Title), value); }
        }

        public string Artist
        {
            get { return GetValue<string>(nameof(Artist), string.Empty); }
            set { SetValue(nameof(Artist), value); }
        }

        public string Path
        {
            get { return GetValue<string>(nameof(Path), string.Empty); }
            set { SetValue(nameof(Path), value); }
        }

        private CurrentPlaySong()
        {
            container = GetOrCreateContainer("CurrentPlaySong");
        }

        private static ApplicationDataContainer GetOrCreateContainer(string key)
        {
            try
            {
                ApplicationDataContainer container;
                return ApplicationData.Current.LocalSettings.Containers.TryGetValue(key, out container)
                    ? container
                    : ApplicationData.Current.LocalSettings.CreateContainer(key,
                        ApplicationDataCreateDisposition.Always);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("GetCurrentPlaySongContainerFail");
                throw;
            }
        }

        private bool TryGetValue<T>(string propertyName, out T value)
        {
            try
            {
                object objValue;
                bool hasValue = container.Values.TryGetValue(propertyName, out objValue) && objValue is T;

                if (hasValue)
                {
                    value = (T)objValue;
                    return true;
                }

                value = default(T);
                return false;
            }
            catch
            {
                value = default(T);
                return false;
            }
        }

        private T GetValue<T>(string propertyName, T defaultValue = default(T))
        {
            T value;
            return TryGetValue(propertyName, out value) ? value : defaultValue;
        }

        private void SetValue(string propertyName, object value)
        {
            try
            {
                IDictionary<string, object> currentValues = container.Values;
                if (currentValues.ContainsKey(propertyName)) currentValues[propertyName] = value;
                else currentValues.Add(propertyName, value);

                OnPropertyChanged(propertyName);
            }
            catch { }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
