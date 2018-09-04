using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace FolderMusicDebug
{
    public class SaveTextClass
    {
        private const int maxLines = 500;

        private static SaveTextClass instance;

        public static string Id { get; set; }

        public static SaveTextClass Current
        {
            get
            {
                if (instance == null) instance = new SaveTextClass();

                return instance;
            }
        }

        private bool savingAllowed = false;
        private readonly object obj = new object();

        public List<DebugEvent> Events { get; set; }

        public SaveTextClass()
        {
            Events = new List<DebugEvent>();
        }

        public void AllowSaving()
        {
            try
            {
                lock (obj)
                {
                    if (!savingAllowed)
                    {
                        savingAllowed = true;

                        try
                        {
                            Task<string> task = PathIO.ReadTextAsync(ViewModel.DebugDataFilepath).AsTask();
                            task.Wait();

                            if (task.Result != "")
                            {
                                Events.InsertRange(0, XmlConverter.Deserialize<SaveTextClass>(task.Result).Events);
                            }
                            else System.Diagnostics.Debug.WriteLine("NoDebugEvents!!!!!!!!!!!!!");
                        }
                        catch
                        {
                            ApplicationData.Current.LocalFolder.CreateFileAsync(ViewModel.DebugDataFilename).AsTask().Wait();
                        }

                        SaveText("AllowSaving");
                    }
                }
            }
            catch { }
        }

        public void SaveText(string name, params object[] objs)
        {
            System.Diagnostics.Debug.WriteLine(name);
            DebugEvent debugEvent = new DebugEvent(name, objs);

            lock (obj)
            {
                Events.Add(debugEvent);

                while (Events.Count > maxLines) Events.RemoveAt(0);
            }

            if (!savingAllowed) return;

            //SaveEvents();
            new Task(SaveEvents).Start();
        }

        private void SaveEvents()
        {
            try
            {
                lock (obj)
                {
                    string xmlText = XmlConverter.Serialize(this);

                    PathIO.WriteTextAsync(ViewModel.DebugDataFilepath, xmlText).AsTask().Wait();
                }
            }
            catch { }               //      */
        }
    }
}
