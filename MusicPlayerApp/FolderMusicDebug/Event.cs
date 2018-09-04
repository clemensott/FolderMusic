using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace MobileDebug
{
    public class Event
    {
        private const char partSplitter = ';', eventSplitter = '|';
        private const int maxLengthOfOneData = 1000;

        private static int count = 0;

        public long Time { get; private set; }

        public int Count { get; private set; }

        public string TaskId { get; private set; }

        public string Name { get; private set; }

        public string[] Data { get; private set; }

        private Event()
        {
            Time = DateTime.Now.Ticks;
            Count = count++;
        }

        internal Event(string name, IEnumerable<object> data) : this()
        {
            Name = name;
            TaskId = Manager.Id;

            Data = data.Select(x => ToString(x)).ToArray();
        }

        public Event(string dataString) : this()
        {
            Time = long.Parse(Manager.GetUntil(ref dataString, partSplitter));
            Count = int.Parse(Manager.GetUntil(ref dataString, partSplitter));
            TaskId = Manager.GetUntil(ref dataString, partSplitter);
            Name = Manager.GetUntil(ref dataString, partSplitter);

            Data = Manager.Split(dataString, partSplitter).ToArray();
        }

        public static IEnumerable<Event> GetEvents(string eventsDataString)
        {
            var array = Manager.Split(eventsDataString, eventSplitter);

            foreach (string eventDataString in array)
            {
                Event debugEvent = null;

                try
                {
                    debugEvent = new Event(eventDataString);
                }
                catch { }

                if (debugEvent != null) yield return debugEvent;
            }
        }

        public string ToDataString()
        {
            string dataString = string.Empty;

            Manager.AddToDataString(ref dataString, Time, partSplitter, eventSplitter);
            Manager.AddToDataString(ref dataString, Count, partSplitter, eventSplitter);
            Manager.AddToDataString(ref dataString, TaskId, partSplitter, eventSplitter);
            Manager.AddToDataString(ref dataString, Name, partSplitter, eventSplitter);

            foreach (string data in Data)
            {
                Manager.AddToDataString(ref dataString, data, partSplitter, eventSplitter);
            }

            return dataString + eventSplitter;
        }

        private string ToString(object obj)
        {
            if (ReferenceEquals(obj, null)) return "RefNull";
            long value;
            string text = obj.ToString();

            if (long.TryParse(text, out value) && value > TimeSpan.TicksPerDay * 10000)
            {
                return GetDateTimeString(value);
            }

            if (text.Length > maxLengthOfOneData) return text.Remove(maxLengthOfOneData);

            return text;
        }

        public static string GetDateTimeString(long ticks)
        {
            var dateTime = new DateTime(ticks);

            return string.Format("{0,2}.{1,2}.{2,4}", dateTime.Day, dateTime.Month, dateTime.Year).Replace(" ", "0")
                + " " + string.Format("{0,2}:{1,2}:{2,2},{3,3}", dateTime.Hour, dateTime.Minute,
                dateTime.Second, dateTime.Millisecond).Replace(" ", "0");
        }

        public override string ToString()
        {
            string[] headerData = new string[] { GetDateTimeString(Time), TaskId, Name };

            return string.Join("\n", headerData.Concat(Data));
        }
    }
}
