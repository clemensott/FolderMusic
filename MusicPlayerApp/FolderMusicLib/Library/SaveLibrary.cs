using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibraryLib
{
    public class SaveLibray
    {
        private static string filename = "Data.xml";
        public int CurrentPlaylistIndex;
        public List<Playlist> Playlists;

        public SaveLibray() { }

        public SaveLibray(int currentPlaylistIndex, List<Playlist> playlists)
        {
            CurrentPlaylistIndex = currentPlaylistIndex;
            Playlists = playlists;
        }

        public static async Task<SaveLibray> Load()
        {
            try
            {
                return await LibraryIO.LoadObject<SaveLibray>(filename);
            }
            catch { }

            return new SaveLibray(-2, new List<Playlist>());
        }

        public async Task Save()
        {
            await LibraryIO.SaveObject(this, filename);
        }

        public static async Task Delete()
        {
            await LibraryIO.Delete(filename);
        }
    }
}