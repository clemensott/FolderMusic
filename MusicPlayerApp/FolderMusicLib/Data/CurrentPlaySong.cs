using System;

namespace MusicPlayer.Data
{
    public struct CurrentPlaySong
    {
        private const string fileName = "CurrentPlaySong.xml";

        public double PositionPercent { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Path { get; set; }

        private CurrentPlaySong(ILibrary library)
        {
            PositionPercent = library.CurrentPlaylist.CurrentSongPositionPercent;

            Title = library.CurrentPlaylist.CurrentSong.Title;
            Artist = library.CurrentPlaylist.CurrentSong.Artist;
            Path = library.CurrentPlaylist.CurrentSong.Path;
        }

        public static void Save(ILibrary library)
        {
            CurrentPlaySong cps = new CurrentPlaySong(library);
            IO.SaveObject(fileName, cps);
        }

        public static void Delete()
        {
            IO.Delete(fileName);
        }

        public static ILibrary Load()
        {
            try
            {
                CurrentPlaySong cps = IO.LoadObject<CurrentPlaySong>(fileName);
                return new Library(cps);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("CurrentPlaySongLoadFail", e);
            }

            return new Library(false);
        }
    }
}
