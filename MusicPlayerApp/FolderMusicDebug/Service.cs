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
        private const char addChar = '&';
        public const string ForegroundId = "Foreground";
        private const string debugDataBackFileName = "FolderMusicBackDebugData.txt",
            debugDataForeFileName = "FolderMusicForeDebugData.txt";
        private static readonly StorageFolder debugFolder = KnownFolders.VideosLibrary;
        private const int maxDebugDataStringLength = 100000, minDebugDataStringLength = 50000;
        private static readonly object lockObj = new object();

        private static bool isAppending = false;
        private static int debugDataStringLength = -1;
        private static StorageFile foreDebugDataFile, backDebugDataFile;

        public static string Id { get; private set; } = "None";

        public static void SetIsForeground()
        {
            Id = ForegroundId;
        }

        public static void SetIsBackground(string id)
        {
            Id = id;
        }

        public static void WriteEvent(string name, IEnumerable data)
        {
            //System.Diagnostics.Debug.WriteLine(name);
            Event debugEvent = new Event(name, data);

            Task.Factory.StartNew(new Action<object>(Append), debugEvent.ToDataString());
        }

        public static void WriteEvent(string name, params object[] data)
        {
            WriteEvent(name, (IEnumerable)data);
        }

        public static void WriteEvent(string name, Exception exc, params object[] data)
        {
            WriteEvent(name, data.Concat(GetMessages(exc)));
        }

        public static void WriteEventPair(string name, IEnumerable data)
        {
            //System.Diagnostics.Debug.WriteLine(name);
            Event debugEvent = Event.GetPair(name, data);

            Task.Factory.StartNew(new Action<object>(Append), debugEvent.ToDataString());
        }

        public static void WriteEventPair(string name, params object[] data)
        {
            WriteEventPair(name, (IEnumerable)data);
        }

        public static void WriteEventPair(string name, Exception exc, params object[] data)
        {
            WriteEvent(name, data.Concat(GetMessagesPair(exc)));
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

        private static IEnumerable<string> GetMessagesPair(Exception e)
        {
            string stackTrace = e.StackTrace;

            while (e != null)
            {
                yield return "Typ: ";
                yield return e.GetType().Name;
                yield return "Mes: ";
                yield return e.Message;

                e = e.InnerException;
            }

            yield return "Stack: ";
            yield return stackTrace;
        }

        private static async void Append(object parameter)
        {
            string text = parameter.ToString();

            SetIsAppending();

            try
            {
                StorageFile file = Id == ForegroundId ? await GetForeDebugDataFile() : await GetBackDebugDataFile();
                await FileIO.AppendTextAsync(file, text);

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

            UnsetIsAppending();
        }

        private static void SetIsAppending()
        {
            lock (lockObj)
            {
                if (isAppending) Monitor.Wait(lockObj);

                isAppending = true;
            }
        }

        private static void UnsetIsAppending()
        {
            lock (lockObj)
            {
                isAppending = false;

                Monitor.Pulse(lockObj);
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
