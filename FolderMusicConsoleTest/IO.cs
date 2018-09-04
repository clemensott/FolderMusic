using System;
using System.IO;

namespace MusicPlayer.Data
{
    internal static class IO
    {
        internal static string LoadText(string skipSongsFileName)
        {
            try
            {
                return File.ReadAllText(skipSongsFileName);
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static void SaveText(string skipSongsFileName, string text)
        {
            File.WriteAllText(skipSongsFileName, text);
        }

        internal static void Delete(string skipSongsFileName)
        {
            File.Delete(skipSongsFileName);
        }
    }
}