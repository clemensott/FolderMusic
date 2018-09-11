using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace MusicPlayer
{
    static class IO
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
                //string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;
                //Task<string> load = PathIO.ReadTextAsync(path).AsTask();
                //load.Wait();
                StorageFile file = GetStorageFile(filenameWithExtention);
                var read = FileIO.ReadTextAsync(file);
                read.AsTask().Wait();

                return read.GetResults();
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("IOLoadTextFail", e, filenameWithExtention);
            }

            return string.Empty;
        }

        public static void SaveObject(string filenameWithExtention, object obj)
        {
            string xmlText = XmlConverter.Serialize(obj);

            if (xmlText != string.Empty) SaveText(filenameWithExtention, xmlText);
            else MobileDebug.Service.WriteEvent("SaveObject", obj.GetType());
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
                    MobileDebug.Service.WriteEvent("IOSaveTextDoubleFail", e, filenameWithExtention);
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
                    MobileDebug.Service.WriteEvent("IOAppendTextDoubleFail", e, filenameWithExtention);
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
                MobileDebug.Service.WriteEvent("IODeleteFail", e, filenameWithExtention);
            }
        }

        public static void Copy(string srcFileName, string destFileName)
        {
            Copy(srcFileName, ApplicationData.Current.LocalFolder, destFileName);
        }

        public static void Copy(string srcFileName, StorageFolder destFolder, string destFileName)
        {
            try
            {
                StorageFile srcFile = GetStorageFile(srcFileName);

                try
                {
                    StorageFile destFile = GetStorageFile(destFileName);

                    srcFile.CopyAndReplaceAsync(destFile);
                }
                catch
                {
                    srcFile.CopyAsync(destFolder, destFileName);
                }
            }
            catch { }
        }

        private static StorageFile GetStorageFile(string fileName)
        {
            return GetStorageFile(ApplicationData.Current.LocalFolder, fileName);
        }

        private static StorageFile GetStorageFile(StorageFolder folder, string fileName)
        {
            var fileOp = folder.GetFileAsync(fileName);
            fileOp.AsTask().Wait();

            return fileOp.GetResults();
        }

        private static StorageFile GetOrCreateStorageFile(string fileName)
        {
            return GetOrCreateStorageFile(ApplicationData.Current.LocalFolder, fileName);
        }

        private static StorageFile GetOrCreateStorageFile(StorageFolder folder, string fileName)
        {
            try
            {
                var fileOp = folder.GetFileAsync(fileName);
                fileOp.AsTask().Wait();

                return fileOp.GetResults();
            }
            catch { }

            var createFileOp = folder.CreateFileAsync(fileName);
            createFileOp.AsTask().Wait();

            return createFileOp.GetResults();
        }
    }
}
