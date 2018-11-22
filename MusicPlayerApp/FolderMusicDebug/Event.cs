using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MobileDebug
{
    public class Event : IEquatable<Event>
    {
        private const char partSplitter = ';', eventSplitter = '|';

        private static int count = 0;

        public long Time { get; private set; }

        public int Count { get; private set; }

        public string BackgroundTaskId { get; private set; }

        public int ThreadId { get; private set; }

        public string Name { get; private set; }

        public string[] Data { get; private set; }

        private Event(string name)
        {
            Time = DateTime.Now.Ticks;
            Count = count++;

            Name = name;
            BackgroundTaskId = Service.Id;
            ThreadId = Environment.CurrentManagedThreadId;
        }

        public Event(string name, IEnumerable<string> data) : this(name)
        {
            Data = data.ToArray();
        }

        internal Event(StringBuilder dataString)
        {
            Time = long.Parse(Service.GetUntil(ref dataString, partSplitter));
            Count = int.Parse(Service.GetUntil(ref dataString, partSplitter));
            BackgroundTaskId = Service.GetUntil(ref dataString, partSplitter);
            ThreadId = int.Parse(Service.GetUntil(ref dataString, partSplitter));
            Name = Service.GetUntil(ref dataString, partSplitter);

            Data = Service.Split(dataString, partSplitter).ToArray();
        }

        internal static IEnumerable<Event> GetEvents(string eventsDataString)
        {
            StringBuilder dataString = new StringBuilder(eventsDataString);
            var dataStrings = Service.Split(dataString, eventSplitter);

            foreach (string eventDataString in dataStrings)
            {
                Event debugEvent;
                StringBuilder eventData = new StringBuilder(eventDataString);

                try
                {
                    debugEvent = new Event(eventData);
                }
                catch
                {
                    continue;
                }

                yield return debugEvent;
            }
        }

        public string ToDataString()
        {
            string dataString = string.Empty;

            Service.AddToDataString(ref dataString, Time, partSplitter, eventSplitter);
            Service.AddToDataString(ref dataString, Count, partSplitter, eventSplitter);
            Service.AddToDataString(ref dataString, BackgroundTaskId, partSplitter, eventSplitter);
            Service.AddToDataString(ref dataString, ThreadId, partSplitter, eventSplitter);
            Service.AddToDataString(ref dataString, Name, partSplitter, eventSplitter);

            foreach (string data in Data)
            {
                Service.AddToDataString(ref dataString, data, partSplitter, eventSplitter);
            }

            return dataString + eventSplitter;
        }

        public static string GetDateTimeString(long ticks)
        {
            var t = new DateTime(ticks);

            string date = string.Format("{0,2}.{1,2}.{2,4}", t.Day, t.Month, t.Year);
            string time = string.Format("{0,2}:{1,2}:{2,2},{3,3}", t.Hour, t.Minute, t.Second, t.Millisecond);

            return date.Replace(" ", "0") + " " + time.Replace(" ", "0");
        }

        public override string ToString()
        {
            string[] headerData = new string[] { GetDateTimeString(Time), BackgroundTaskId, ThreadId.ToString(), Name };

            return string.Join("\n", headerData.Concat(Data));
        }

        public bool Equals(Event other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (BackgroundTaskId != other.BackgroundTaskId) return false;
            if (Count != other.Count) return false;
            if (!Data.SequenceEqual(other.Data)) return false;
            if (Name != other.Name) return false;
            if (ThreadId != other.ThreadId) return false;
            if (Time != other.Time) return false;

            return true;
        }

        public static bool operator ==(Event e1, Event e2)
        {
            return (e1?.Equals(e2) ?? e2?.Equals(e1)) ?? true;
        }

        public static bool operator !=(Event e1, Event e2)
        {
            return !(e1 == e2);
        }
    }
}
