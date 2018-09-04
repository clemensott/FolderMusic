using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MobileDebug
{
    public static class Manager
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

        public static void WriteEvent(string name, IEnumerable<object> data)
        {
            //System.Diagnostics.Debug.WriteLine(name);
            Event debugEvent = new Event(name, data);

            new Task(new Action<object>(Append), debugEvent.ToDataString()).Start();
        }

        public static void WriteEvent(string name, params object[] data)
        {
            WriteEvent(name, (IEnumerable<object>)data);
        }

        public static void WriteEvent(string name, Exception exc, params object[] data)
        {
            WriteEvent(name, data.Concat(GetMessages(exc)));
        }

        private static IEnumerable<string> GetMessages(Exception e)
        {
            while (e != null)
            {
                yield return "Typ: " + e.GetType().Name;
                yield return "Mes: " + e.Message;
                e = e.InnerException;
            }
        }

        private static async void Append(object parameter)
        {
            string text = parameter.ToString();

            lock (lockObj)
            {
                while (isAppending) Task.Delay(10).Wait(20);
                isAppending = true;
            }

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

            isAppending = false;
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

        internal static IEnumerable<string> Split(string dataString, char seperator)
        {
            while (dataString.Length > 0)
            {
                yield return GetUntil(ref dataString, seperator);
            }
        }

        internal static string GetUntil(ref string text, char seperator)
        {
            int lenght = 0;
            string part = string.Empty;

            while (true)
            {
                char c = text.ElementAtOrDefault(lenght);
                if (c == seperator)
                {
                    lenght++;

                    if (text.ElementAtOrDefault(lenght) != addChar) break;
                }

                part += c;
                lenght++;

                if (lenght >= text.Length) break;
            }

            text = text.Remove(0, lenght);

            return part;
        }

        internal static void AddToDataString(ref string dataString, object add, char dataSplitter, params char[] splitters)
        {
            string addText = add.ToString();

            foreach (char splitter in splitters)
            {
                addText = addText.Replace(splitter.ToString(), splitter.ToString() + addText.ToString());
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
