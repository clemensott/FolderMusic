using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FolderMusicUwpLib
{
    public class SaveTextClass
    {
        private static string id;

        public static void SetId(string getId)
        {
            id = getId;
        }

        public async static void SaveText(params object[] objs)
        {
            var list = new List<object>();

            list.Add(DateTime.Now.Ticks);
            list.AddRange(objs);
            list.Add(id);

            try
            {
                string text = "";
                string filename = string.Format("Text{0}.txt", new Random().Next(10, 1000));
                StorageFile file = await ApplicationData.Current.LocalFolder.
                    CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                foreach (object obj in list) text += obj.ToString() + ";";

                text = text.TrimEnd(';');

                await PathIO.WriteTextAsync(file.Path, text);
            }
            catch { }
        }
    }
}
