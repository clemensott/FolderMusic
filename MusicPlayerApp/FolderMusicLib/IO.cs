using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace MusicPlayer
{
    static class IO
    {
        public async static Task<T> LoadObjectAsync<T>(string filenameWithExtention)
        {
            string xmlText = await LoadTextAsync(filenameWithExtention);

            return XmlConverter.Deserialize<T>(xmlText);
        }

        public async static Task<string> LoadTextAsync(string filenameWithExtention)
        {
            try
            {
                StorageFile file = await GetStorageFileAsync(filenameWithExtention);

                return await FileIO.ReadTextAsync(file);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("IOLoadTextFail", e, filenameWithExtention);
            }

            return string.Empty;
        }

        public async static Task SaveObjectAsync(string filenameWithExtention, object obj)
        {
            string xmlText = XmlConverter.Serialize(obj);

            if (xmlText != string.Empty) await SaveTextAsync(filenameWithExtention, xmlText);
            else MobileDebug.Service.WriteEvent("SaveObject", obj.GetType());
        }

        public async static Task SaveTextAsync(string filenameWithExtention, string text)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;

            try
            {
                StorageFile file = await GetOrCreateStorageFileAsync(filenameWithExtention);
                await FileIO.WriteTextAsync(file, text);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("IOSaveTextFail", e, filenameWithExtention);
            }
        }

        public async static Task AppendTextAsync(string filenameWithExtention, string text)
        {
            try
            {
                StorageFile file = await GetOrCreateStorageFileAsync(filenameWithExtention);
                await FileIO.AppendTextAsync(file, text);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("IOAppendTextDoubleFail", e, filenameWithExtention);
            }
        }

        public async static Task DeleteAsync(string filenameWithExtention)
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

        public async static Task CopyAsync(string srcFileName, string destFileName)
        {
            await CopyAsync(srcFileName, ApplicationData.Current.LocalFolder, destFileName);
        }

        public async static Task CopyAsync(string srcFileName, StorageFolder destFolder, string destFileName)
        {
            StorageFile srcFile;

            try
            {
                srcFile = await GetStorageFileAsync(srcFileName);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEventPair("IOCopyGetSrcFail", e, "Src: ", srcFileName,
                    "DestFolder: ", destFolder.Path, "DestFile: ", destFileName);
                return;
            }

            try
            {
                StorageFile destFile = await GetStorageFileAsync(destFileName);

                await srcFile.CopyAndReplaceAsync(destFile);
            }
            catch (FileNotFoundException e)
            {
                MobileDebug.Service.WriteEventPair("IOCopyFileNotFound", e, "Src: ", srcFileName,
                    "DestFolder: ", destFolder.Path, "DestFile: ", destFileName);

                await srcFile.CopyAsync(destFolder, destFileName);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEventPair("IOCopyFail", e, "Src: ", srcFileName,
                    "DestFolder: ", destFolder.Path, "DestFile: ", destFileName);
            }
        }

        private async static Task<StorageFile> GetStorageFileAsync(string fileName)
        {
            return await GetStorageFileAsync(ApplicationData.Current.LocalFolder, fileName);
        }

        private async static Task<StorageFile> GetStorageFileAsync(StorageFolder folder, string fileName)
        {
            return await folder.GetFileAsync(fileName);
        }

        private async static Task<StorageFile> GetOrCreateStorageFileAsync(string fileName)
        {
            return await GetOrCreateStorageFileAsync(ApplicationData.Current.LocalFolder, fileName);
        }

        private async static Task<StorageFile> GetOrCreateStorageFileAsync(StorageFolder folder, string fileName)
        {
            try
            {
                return await folder.GetFileAsync(fileName);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("GetOrCreateStorageFileAsyncFail", e, folder.Path, fileName);
            }

            return await folder.CreateFileAsync(fileName);
        }
    }
}
