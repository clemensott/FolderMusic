using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace PlaylistSong
{
    public class SaveClass
    {
        private static string filename = "Data.xml";
        public int CurrentPlaylistIndex;
        public SavePlaylist[] Playlists;

        public SaveClass() { }

        public SaveClass(int currentPlaylistIndex, List<Playlist> playlists)
        {
            CurrentPlaylistIndex = currentPlaylistIndex;
            Playlists = new SavePlaylist[playlists.Count];

            for (int i = 0; i < playlists.Count; i++)
            {
                Playlists[i] = new SavePlaylist(playlists[i]);
            }
        }

        public static async Task<SaveClass> Load()
        {
            string xmlFileText;
            List<Playlist> list = new List<Playlist>();
            SaveClass sc = new SaveClass(-2, list);

            try
            {
                var path = ApplicationData.Current.LocalFolder.Path + "\\" + filename;
                XmlSerializer serializer = new XmlSerializer(typeof(SaveClass));

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        xmlFileText = await PathIO.ReadTextAsync(path);

                        TextReader tr = new StringReader(xmlFileText);

                        object obj = serializer.Deserialize(tr);
                        sc = obj as SaveClass;

                        return sc;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        await Task.Delay(100);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return sc;
        }

        public async void Save()
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filename;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SaveClass));

                    TextWriter tw = new StringWriter();
                    serializer.Serialize(tw, this);

                    TextReader tr = new StringReader(tw.ToString());
                    serializer.Deserialize(tr);

                    await PathIO.WriteTextAsync(path, tw.ToString());

                    return;
                }
                catch (FileNotFoundException e)
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync(filename);
                    i--;
                }
                catch (Exception e) { }
            }
        }
    }
}