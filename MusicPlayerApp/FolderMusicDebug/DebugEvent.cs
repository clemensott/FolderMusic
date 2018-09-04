using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.UI.Core;

namespace FolderMusicDebug
{
    public class DebugEvent
    {
        private const int maxLengthOfOneData = 30;

        public bool IsChecked { get; set; }

        public long Time { get; set; }

        public string TaskId { get; set; }

        public string Name { get; set; }

        public string[] Data { get; set; }

        public DebugEvent()
        {
            IsChecked = true;
            Time = DateTime.Now.Ticks;
        }

        public DebugEvent(string name, object[] data) : this()
        {
            Name = name;
            TaskId = SaveTextClass.Id;

            Data = data.Select(x => ToString(x)).ToArray();
        }

        private string ToString(object obj)
        {
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
            string output = string.Format("{0}\n{1}\n{2}\n", GetDateTimeString(Time), TaskId, Name);

            foreach (string data in Data) output += data + "\n";

            return output.TrimEnd('\n');
        }
    }
}
