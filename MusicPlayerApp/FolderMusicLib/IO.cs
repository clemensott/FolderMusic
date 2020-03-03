using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace MusicPlayer
{
    static class IO
    {
        public static async Task<T> LoadObjectAsync<T>(string filenameWithExtension)
        {
            string xmlText = await LoadTextAsync(filenameWithExtension);

            return XmlConverter.Deserialize<T>(xmlText);
        }

        public static async Task<string> LoadTextAsync(string filenameWithExtension)
        {
            try
            {
                StorageFile file = await GetStorageFileAsync(filenameWithExtension);

                return await FileIO.ReadTextAsync(file);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("IOLoadTextFail", e, filenameWithExtension);
            }

            return string.Empty;
        }

        public static async Task SaveObjectAsync(string filenameWithExtension, object obj)
        {
            string xmlText = XmlConverter.Serialize(obj);

            if (xmlText != string.Empty) await SaveTextAsync(filenameWithExtension, xmlText);
            else MobileDebug.Service.WriteEvent("SaveObject", obj.GetType());
        }

        public static async Task SaveTextAsync(string filenameWithExtension, string text)
        {
            StorageFile file = null;
            try
            {
                file = await GetOrCreateStorageFileAsync(filenameWithExtension);
                await FileIO.WriteTextAsync(file, text);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("IOSaveTextFail", e, filenameWithExtension, file?.Path);
            }
        }

        public static async Task AppendTextAsync(string filenameWithExtension, string text)
        {
            try
            {
                StorageFile file = await GetOrCreateStorageFileAsync(filenameWithExtension);
                await FileIO.AppendTextAsync(file, text);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("IOAppendTextDoubleFail", e, filenameWithExtension);
            }
        }

        public static async Task DeleteAsync(string filenameWithExtension)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filenameWithExtension);

                await file.DeleteAsync();
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("IODeleteFail", e, filenameWithExtension);
            }
        }

        public static async Task CopyAsync(string srcFileName, string destFileName)
        {
            await CopyAsync(srcFileName, ApplicationData.Current.LocalFolder, destFileName);
        }

        public static async Task CopyAsync(string srcFileName, StorageFolder destFolder, string destFileName)
        {
            StorageFile srcFile;

            try
            {
                srcFile = await GetStorageFileAsync(srcFileName);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEventPair("IOCopyGetSrcFail", "Src", srcFileName,
                    "DestFolder", destFolder.Path, "DestFile", destFileName, e);
                return;
            }

            try
            {
                StorageFile destFile = await GetStorageFileAsync(destFileName);

                await srcFile.CopyAndReplaceAsync(destFile);
            }
            catch (FileNotFoundException e)
            {
                MobileDebug.Service.WriteEventPair("IOCopyFileNotFound", "Src", srcFileName,
                    "DestFolder", destFolder.Path, "DestFile", destFileName, e);

                await srcFile.CopyAsync(destFolder, destFileName);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEventPair("IOCopyFail", "Src", srcFileName,
                    "DestFolder", destFolder.Path, "DestFile", destFileName, e);
            }
        }

        private static async Task<StorageFile> GetStorageFileAsync(string fileName)
        {
            return await GetStorageFileAsync(ApplicationData.Current.LocalFolder, fileName);
        }

        private static async Task<StorageFile> GetStorageFileAsync(StorageFolder folder, string fileName)
        {
            return await folder.GetFileAsync(fileName);
        }

        private static async Task<StorageFile> GetOrCreateStorageFileAsync(string fileName)
        {
            return await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
        }
    }
}
