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
        private static readonly object lockObj = new object();

        private static int debugDataStringLength = -1;
        private static StorageFile foreDebugDataFile, backDebugDataFile;
        private static Queue<Event> eventsBuffer = new Queue<Event>();
        private static Task writeTask = Task.Run((Action)Append);

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
            WriteEvent(name, data, GetMessages(exc));
        }

        public static void WriteEventPair(string name, IEnumerable data)
        {
            //System.Diagnostics.Debug.WriteLine(name);
            WriteEvent(name, ConcatPair(ToEnumerable(data)));
        }

        public static void WriteEventPair(string name, params object[] data)
        {
            WriteEventPair(name, data.AsEnumerable());
        }

        public static void WriteEventPair(string name, Exception exc, params object[] data)
        {
            WriteEvent(name, ConcatPair(ToEnumerable(data)), GetMessages(exc));
        }

        private static IEnumerable<string> GetMessages(Exception e)
        {
            string stackTrace = e.StackTrace;

            while (e != null)
            {
                yield return "Typ: " + e.GetType().Name;
                yield return "Mes: " + e.Message;

                e = e.InnerException;
            }

            yield return "Stack: " + stackTrace;
        }

        private static IEnumerable<string> ToEnumerable(string text)
        {
            yield return text;
        }

        private static IEnumerable<string> ToEnumerable(IEnumerable data)
        {
            foreach (object obj in data)
            {
                if (!(obj is IEnumerable) || obj is string) yield return ToString(obj);
                else
                {
                    foreach (string text in ToEnumerable((IEnumerable)obj)) yield return text;
                }
            }
        }

        private static IEnumerable<string> ConcatPair(IEnumerable<string> data)
        {
            bool isFirst = true;
            string last = string.Empty;

            foreach (string text in data)
            {
                if (isFirst) last = text;
                else yield return last + text;

                isFirst = !isFirst;
            }

            if (!isFirst) yield return last;
        }

        private static string ToString(object obj)
        {
            if (ReferenceEquals(obj, null)) return NullReferenceValue;
            long value;
            string text = obj.ToString();

            if (text.Length > maxLengthOfOneData) return text.Remove(maxLengthOfOneData) + "[...]";

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

        private async static void Append()
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

        internal static string GetUntil(ref StringBuilder text, char seperator)
        {
            int length;
            StringBuilder part = new StringBuilder(text.Length);

            for (length = 0; length < text.Length; length++)
            {
                char c = text[length];

                if (text[length] == seperator)
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
