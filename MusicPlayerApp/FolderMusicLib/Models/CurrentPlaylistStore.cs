using System;
using System.Collections.Generic;
using Windows.Storage;
using MusicPlayer.Models.Enums;

namespace MusicPlayer.Models
{
    public class CurrentPlaylistStore
    {
        private static CurrentPlaylistStore instance;

        public static CurrentPlaylistStore Current
        {
            get
            {
                if (instance == null) instance = new CurrentPlaylistStore();

                return instance;
            }
        }

        private readonly ApplicationDataContainer container;

        public long PositionTicks
        {
            get { return GetValue<long>(nameof(PositionTicks)); }
            set { SetValue(nameof(PositionTicks), value); }
        }

        public Song? CurrentSong
        {
            get { return Utils.XmlDeserializeSong(GetValue<string>(nameof(CurrentSong))); }
            set { SetValue(nameof(CurrentSong), Utils.XmlSerialize(value)); }
        }

        public LoopType Loop
        {
            get { return (LoopType)GetValue<byte>(nameof(Loop)); }
            set { SetValue(nameof(Loop), (byte)value); }
        }

        public string SongsHash
        {
            get { return GetValue<string>(nameof(SongsHash)); }
            set { SetValue(nameof(SongsHash), value); }
        }

        private CurrentPlaylistStore()
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
                MobileDebug.Service.WriteEvent("GetCurrentPlaySongContainerFail", e);
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
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEventPair("CurrentPlaySong.SetValueFail",
                    "prop", propertyName, "valueType", value?.GetType().FullName, e);
            }
        }
    }
}
