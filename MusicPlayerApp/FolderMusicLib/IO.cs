using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace MusicPlayer
{
    class IO
    {
        public static T LoadObject<T>(string filenameWithExtention)
        {
            string xmlText = LoadText(filenameWithExtention);

            //FolderMusicDebug.DebugEvent.SaveText("LoadObject", typeof(T).Name, filenameWithExtention, xmlText, xmlText.Length);
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
                MobileDebug.Manager.WriteEvent("IOLoadTextFail", e, filenameWithExtention);
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
                catch (Exception e)
                {
                    MobileDebug.Manager.WriteEvent("IOSaveTextDoubleFail", e, filenameWithExtention);
                }
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
                catch (Exception e)
                {
                    MobileDebug.Manager.WriteEvent("IOAppendTextDoubleFail", e, filenameWithExtention);
                }
            }
        }

        public async static void Delete(string filenameWithExtention)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filenameWithExtention);

                await file.DeleteAsync();
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("IODeleteFail", e, filenameWithExtention);
            }
        }
    }
}
