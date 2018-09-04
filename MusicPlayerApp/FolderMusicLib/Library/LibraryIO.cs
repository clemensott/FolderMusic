using FolderMusicLib;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace LibraryLib
{
    class LibraryIO
    {
        public static T LoadObject<T>(string filenameWithExtention)
        {
            string xmlText = LoadText(filenameWithExtention);

            return XmlConverter.Deserialize<T>(xmlText);
        }

        public static string LoadText(string filenameWithExtention)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;
            Task<string> load = PathIO.ReadTextAsync(path).AsTask();
            load.Wait();

            return load.Result;
        }

        public static void SaveObject(object obj, string filenameWithExtention)
        {
            string xmlText = XmlConverter.Serialize(obj);

            if (xmlText == "") return;

            SaveText(xmlText, filenameWithExtention);
        }

        public static void SaveText(string text, string filenameWithExtention)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;

            try
            {
                PathIO.WriteTextAsync(path, text).AsTask().Wait();
            }
            catch (Exception e)
            {
                try
                {
                    ApplicationData.Current.LocalFolder.CreateFileAsync(filenameWithExtention).AsTask().Wait();

                    PathIO.WriteTextAsync(path, text).AsTask().Wait();
                }
                catch { }
            }
        }

        public static void AppendText(string text, string filenameWithExtention)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;

            try
            {
                PathIO.AppendTextAsync(path, text).AsTask().Wait();
            }
            catch (FileNotFoundException)
            {
                try
                {
                    ApplicationData.Current.LocalFolder.CreateFileAsync(filenameWithExtention).AsTask().Wait();

                    PathIO.WriteTextAsync(path, text).AsTask().Wait();
                }
                catch { }
            }
        }

        public async static void Delete(string filenameWithExtention)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filenameWithExtention);

                await file.DeleteAsync();
            }
            catch { }
        }
    }
}
