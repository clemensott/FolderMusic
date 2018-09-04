using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace MusicPlayer.Data
{
    public class SaveLibray
    {
        private static string filename = "Data.xml", filenameBackup = "DataBackup.xml";

        public int CurrentPlaylistIndex;
        public List<Playlist> Playlists;

        public SaveLibray() { }

        public SaveLibray(int currentPlaylistIndex, List<Playlist> playlists)
        {
            CurrentPlaylistIndex = currentPlaylistIndex;
            Playlists = playlists;
        }

        public static SaveLibray Load()
        {
            try
            {
                SaveLibray lib = IO.LoadObject<SaveLibray>(filename);
                CreateBackup();

                return lib;
            }
            catch (Exception e)
            {
                Exception inner = e.InnerException;
            }

            return null;
        }

        private static async Task CreateBackup()
        {
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);

            try
            {
                StorageFile dataBackupFile = await ApplicationData.Current.LocalFolder.GetFileAsync(filenameBackup);

                try
                {
                    await dataFile.CopyAndReplaceAsync(dataBackupFile);
                }
                catch { }
            }
            catch
            {
                await dataFile.CopyAsync(ApplicationData.Current.LocalFolder, filenameBackup);
            }
        }

        public static SaveLibray LoadBackup()
        {
            try
            {
                SaveLibray lib = IO.LoadObject<SaveLibray>(filenameBackup);
                RestoreBackup();

                return lib;
            }
            catch { }

            return null;
        }

        private static async Task RestoreBackup()
        {
            StorageFile dataBackupFile = await ApplicationData.Current.LocalFolder.GetFileAsync(filenameBackup);

            try
            {
                StorageFile dataFile = await ApplicationData.Current.LocalFolder.GetFileAsync(filenameBackup);

                try
                {
                    await dataBackupFile.CopyAndReplaceAsync(dataFile);
                }
                catch { }
            }
            catch
            {
                await dataBackupFile.CopyAsync(ApplicationData.Current.LocalFolder, filename);
            }
        }

        public void Save()
        {
            lock (Library.Current)
            {
                IO.SaveObject(filename, this);
            }
        }

        public static void Delete()
        {
            IO.Delete(filename);
        }
    }
}