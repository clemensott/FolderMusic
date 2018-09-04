using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace LibraryLib
{
    public class SaveClass
    {
        private static string filename = "Data.xml";
        public int CurrentPlaylistIndex;
        public List<Playlist> Playlists;

        public SaveClass() { }

        public SaveClass(int currentPlaylistIndex, List<Playlist> playlists)
        {
            CurrentPlaylistIndex = currentPlaylistIndex;
            Playlists = playlists;
        }

        public static async Task<SaveClass> Load()
        {
            string xmlFileText;
            List<Playlist> list = new List<Playlist>();

            try
            {
                var path = ApplicationData.Current.LocalFolder.Path + "\\" + filename;

                try
                {
                    xmlFileText = await PathIO.ReadTextAsync(path);

                    return XmlConverter.Deserialize<SaveClass>(xmlFileText);
                }
                catch { }
            }
            catch { }

            return new SaveClass(-2, list);
        }

        public async Task Save()
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filename;
            string xmlFileText;

            try
            {
                xmlFileText = XmlConverter.Serialize(this);

                await PathIO.WriteTextAsync(path, xmlFileText);
            }
            catch (FileNotFoundException)
            {
                try
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(filename);

                    xmlFileText = XmlConverter.Serialize(this);

                    await PathIO.WriteTextAsync(path, xmlFileText);
                }
                catch { }
            }
            catch { }

            return;
        }
    }
}