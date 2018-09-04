using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace MusicPlayer.Data
{
    class IO
    {
        public static T LoadObject<T>(string filenameWithExtention)
        {
            string xmlText = LoadText(filenameWithExtention);

            return XmlConverter.Deserialize<T>(xmlText);
        }

        public static string LoadText(string filenameWithExtention)
        {
            try
            {
                string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;
                Task<string> load = PathIO.ReadTextAsync(path).AsTask();
                load.Wait();

                return load.Result;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(filenameWithExtention);
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            return string.Empty;
        }

        public static void SaveObject(string filenameWithExtention, object obj)
        {
            string xmlText = XmlConverter.Serialize(obj);

            if (xmlText == string.Empty) return;

            SaveText(filenameWithExtention, xmlText);
        }

        public static void SaveText(string filenameWithExtention, string text)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;

            try
            {
                PathIO.WriteTextAsync(path, text).AsTask().Wait();
            }
            catch
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
            catch
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
