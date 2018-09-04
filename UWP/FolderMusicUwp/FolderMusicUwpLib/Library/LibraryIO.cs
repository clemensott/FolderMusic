using FolderMusicUwpLib;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace LibraryLib
{
    class LibraryIO
    {
        public async static Task<T> LoadObject<T>(string filenameWithExtention)
        {
            return XmlConverter.Deserialize<T>(await LoadText(filenameWithExtention));
        }

        public async static Task<string> LoadText(string filenameWithExtention)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;

            return await PathIO.ReadTextAsync(path);
        }

        public async static Task SaveObject(object obj, string filenameWithExtention)
        {
            string xmlText = XmlConverter.Serialize(obj);

            if (xmlText == "") return;

            await SaveText(xmlText, filenameWithExtention);
        }

        public async static Task SaveText(string text, string filenameWithExtention)
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;

            try
            {
                await PathIO.WriteTextAsync(path, text);
            }
            catch (FileNotFoundException)
            {
                try
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(filenameWithExtention);

                    await PathIO.WriteTextAsync(path, text);
                }
                catch { }
            }
        }

        public async static Task Delete(string filenameWithExtention)
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
