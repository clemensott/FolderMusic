using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace MobileDebug
{
    public static class Service
    {
        private const int maxLengthOfOneData = 1000;
        private const char addChar = '&';
        public const string ForegroundId = "Foreground";
        private const string debugDataBackFileName = "FolderMusicBackDebugData.txt",
            debugDataForeFileName = "FolderMusicForeDebugData.txt";
        public const string NullReferenceValue = "RefNull";

        private static readonly StorageFolder debugFolder = KnownFolders.VideosLibrary;
        private const int maxDebugDataStringLength = 100000, minDebugDataStringLength = 50000;

        private static int debugDataStringLength = -1;
        private static StorageFile foreDebugDataFile, backDebugDataFile;
        private static readonly Queue<Event> eventsBuffer = new Queue<Event>();
        private static Task writeTask = Task.Run(Append);

        public static string Id { get; private set; } = "None";

        public static void SetIsForeground()
        {
            Id = ForegroundId;
        }

        public static void SetIsBackground(string id)
        {
            Id = id;
        }

        public static void WriteEvent(string name, string text)
        {
            //System.Diagnostics.Debug.WriteLine(name);
            Event debugEvent = new Event(name, ToEnumerable(text));

            Append(debugEvent);
        }

        public static void WriteEvent(string name, IEnumerable data)
        {
            //System.Diagnostics.Debug.WriteLine(name);
            Event debugEvent = new Event(name, ToEnumerable(data));

            Append(debugEvent);
        }

        public static void WriteEvent(string name, params object[] data)
        {
            WriteEvent(name, data.AsEnumerable());
        }

        public static void WriteEvent(string name, Exception exc, params object[] data)
        {
            WriteEvent(name, data, exc.ToString());
        }

        public static void WriteEventPair(string name, string key0, object value0, Exception exception = null)
        {
            WriteEventPair(name, new[] { GetPair(key0, value0) }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0,
            string key1, object value1, Exception exception = null)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value0),
                GetPair(key1, value1),
            }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0,
            string key1, object value1, string key2, object value2, Exception exception = null)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value0),
                GetPair(key1, value1),
                GetPair(key2, value2),
            }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0, string key1, object value1,
            string key2, object value2, string key3, object value3, Exception exception = null)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value0),
                GetPair(key1, value1),
                GetPair(key2, value2),
                GetPair(key3, value3),
            }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0,
            string key1, object value1, string key2, object value2, string key3,
            object value3, string key4, object value4, Exception exception = null)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value0),
                GetPair(key1, value1),
                GetPair(key2, value2),
                GetPair(key3, value3),
                GetPair(key4, value4),
            }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0, string key1,
            object value1, string key2, object value2, string key3, object value3, string key4,
            object value4, string key5, object value5, Exception exception = null)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value0),
                GetPair(key1, value1),
                GetPair(key2, value2),
                GetPair(key3, value3),
                GetPair(key4, value4),
                GetPair(key5, value5),
            }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0, string key1,
            object value1, string key2, object value2, string key3, object value3, string key4,
            object value4, string key5, object value5, string key6, object value6, Exception exception = null)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value0),
                GetPair(key1, value1),
                GetPair(key2, value2),
                GetPair(key3, value3),
                GetPair(key4, value4),
                GetPair(key5, value5),
                GetPair(key6, value6),
            }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0, string key1, object value1,
            string key2, object value2, string key3, object value3, string key4, object value4, string key5,
            object value5, string key6, object value6, string key7, object value7, Exception exception = null)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value0),
                GetPair(key1, value1),
                GetPair(key2, value2),
                GetPair(key3, value3),
                GetPair(key4, value4),
                GetPair(key5, value5),
                GetPair(key6, value6),
                GetPair(key7, value7),
            }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0, string key1,
            object value1, string key2, object value2, string key3, object value3, string key4,
            object value4, string key5, object value5, string key6, object value6, string key7,
            object value7, string key8, object value8, Exception exception = null)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value0),
                GetPair(key1, value1),
                GetPair(key2, value2),
                GetPair(key3, value3),
                GetPair(key4, value4),
                GetPair(key5, value5),
                GetPair(key6, value6),
                GetPair(key7, value7),
                GetPair(key8, value8),
            }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0, string key1, object value1,
            string key2, object value2, string key3, object value3, string key4, object value4, string key5,
            object value5, string key6, object value6, string key7, object value7, string key8,
            object value8, string key9, object value9, Exception exception = null)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value0),
                GetPair(key1, value1),
                GetPair(key2, value2),
                GetPair(key3, value3),
                GetPair(key4, value4),
                GetPair(key5, value5),
                GetPair(key6, value6),
                GetPair(key7, value7),
                GetPair(key8, value8),
                GetPair(key9, value9),
            }, exception);
        }

        public static void WriteEventPair(string name, string key0, object value0, string key1, object value1,
            string key2, object value2, string key3, object value3, string key4, object value4, string key5,
            object value5, string key6, object value6, string key7, object value7, string key8,
            object value8, string key9, object value9, Exception exception = null, params object[] data)
        {
            WriteEventPair(name, new[]
            {
                GetPair(key0, value1),
                GetPair(key1, value1),
                GetPair(key2, value2),
                GetPair(key3, value3),
                GetPair(key4, value4),
                GetPair(key5, value5),
                GetPair(key6, value6),
                GetPair(key7, value7),
                GetPair(key8, value8),
                GetPair(key9, value9),
            }.Concat(ToEnumerable(data)), exception);
        }

        private static string GetPair(string key, object value)
        {
            return string.Format("{0}: {1}", key, ToStringAndTrim(value));
        }

        public static void WriteEventPair(string name, IEnumerable<string> data, Exception exception)
        {
            //System.Diagnostics.Debug.WriteLine(name);
            if (exception != null) data = data.Concat(ToEnumerable(exception.ToString()));
            WriteEvent(name, data);
        }

        private static IEnumerable<string> ToEnumerable(string text)
        {
            yield return text;
        }

        private static IEnumerable<string> ToEnumerable(IEnumerable data)
        {
            foreach (object obj in data)
            {
                if (!(obj is IEnumerable) || obj is string) yield return ToStringAndTrim(obj);
                else
                {
                    foreach (string text in ToEnumerable((IEnumerable)obj)) yield return text;
                }
            }
        }

        private static string ToStringAndTrim(object obj)
        {
            if (ReferenceEquals(obj, null)) return NullReferenceValue;
            string text = obj.ToString();

            if (text.Length > maxLengthOfOneData)
            {
                int restLength = text.Length - maxLengthOfOneData;
                return string.Format("{0} [{1}]", text.Remove(maxLengthOfOneData), restLength);
            }

            return text;
        }

        private static void Append(Event debugEvent)
        {
            lock (eventsBuffer)
            {
                eventsBuffer.Enqueue(debugEvent);

                if (eventsBuffer.Count == 1) Monitor.Pulse(eventsBuffer);
            }
        }

        private static async Task Append()
        {
            while (true)
            {
                Event e;
                string text = string.Empty;

                lock (eventsBuffer)
                {
                    while (eventsBuffer.Count == 0) Monitor.Wait(eventsBuffer);

                    do
                    {
                        e = eventsBuffer.Dequeue();
                        text += e.ToDataString();
                    }
                    while (eventsBuffer.Count > 0);
                }

                //System.Diagnostics.Debug.WriteLine(e.Name + "Before");
                try
                {
                    StorageFile file = Id == ForegroundId ? await GetForeDebugDataFile() : await GetBackDebugDataFile();
                    await FileIO.AppendTextAsync(file, text);
                    //System.Diagnostics.Debug.WriteLine(e.Name + "After");

                    if (eventsBuffer.Count > 0) continue;

                    if (debugDataStringLength == -1) debugDataStringLength = (await FileIO.ReadTextAsync(file)).Length;
                    if (debugDataStringLength > maxDebugDataStringLength)
                    {
                        string completeText = await FileIO.ReadTextAsync(file);
                        debugDataStringLength = completeText.Length;

                        if (debugDataStringLength > maxDebugDataStringLength)
                        {
                            int surplus = completeText.Length - minDebugDataStringLength;
                            await FileIO.WriteTextAsync(file, completeText.Remove(0, surplus));
                            debugDataStringLength = minDebugDataStringLength;
                        }
                    }
                }
                catch { }
            }
        }

        internal static async Task<StorageFile> GetBackDebugDataFile()
        {
            if (backDebugDataFile != null) return backDebugDataFile;

            try
            {
                backDebugDataFile = await debugFolder.GetFileAsync(debugDataBackFileName);
            }
            catch (FileNotFoundException)
            {
                backDebugDataFile = await debugFolder.CreateFileAsync(debugDataBackFileName);
            }

            return backDebugDataFile;
        }

        internal static async Task<StorageFile> GetForeDebugDataFile()
        {
            if (foreDebugDataFile != null) return foreDebugDataFile;

            try
            {
                foreDebugDataFile = await debugFolder.GetFileAsync(debugDataForeFileName);
            }
            catch (FileNotFoundException)
            {
                foreDebugDataFile = await debugFolder.CreateFileAsync(debugDataForeFileName);
                await FileIO.WriteTextAsync(foreDebugDataFile, "0");
            }

            return foreDebugDataFile;
        }

        internal static IEnumerable<string> Split(StringBuilder dataString, char seperator)
        {
            while (dataString.Length > 0)
            {
                yield return GetUntil(ref dataString, seperator);
            }
        }

        internal static string GetUntil(ref StringBuilder text, char separator)
        {
            int length;
            StringBuilder part = new StringBuilder(text.Length);

            for (length = 0; length < text.Length; length++)
            {
                char c = text[length];

                if (text[length] == separator)
                {
                    length++;

                    if (length >= text.Length || text[length] != addChar) break;
                }

                part.Append(c);
            }

            text.Remove(0, length);

            return part.ToString();
        }

        internal static void AddToDataString(ref string dataString, object add, char dataSplitter, params char[] splitters)
        {
            string addText = add.ToString().Replace(dataSplitter.ToString(), dataSplitter.ToString() + addChar.ToString());

            foreach (char splitter in splitters)
            {
                addText = addText.Replace(splitter.ToString(), splitter.ToString() + addChar.ToString());
            }

            dataString += addText + dataSplitter;
        }

        public static string GetDataEventsString(this IEnumerable<Event> events)
        {
            string dataString = string.Empty;

            foreach (Event debugEvent in events)
            {
                dataString += debugEvent.ToDataString();
            }

            return dataString;
        }
    }
}
