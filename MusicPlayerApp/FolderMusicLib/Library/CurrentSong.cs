using System.Threading.Tasks;
using Windows.Media.Playback;

namespace LibraryLib
{
    public class CurrentSong
    {
        private const string currentSongMillisecondsFileName = "CurrentSongMilliseconds.txt",
        currentSongFileName = "CurrentSong.xml";

        private static CurrentSong instance;

        public static CurrentSong Current
        {
            get
            {
                if (instance == null) instance = new CurrentSong();

                return instance;
            }
        }

        private bool loaded;
        private double positionPercent = 0;
        private Song song;

        public bool IsLoaded { get { return loaded; } }

        public double PositionPercent { get { return positionPercent; } }

        public Song Song { get { return song != null ? song : new Song(); } }

        private CurrentSong() { }

        public void Unset()
        {
            loaded = false;
            song = null;
            positionPercent = 0;

            LibraryIO.Delete(currentSongFileName);
            LibraryIO.Delete(currentSongMillisecondsFileName);
        }

        public void Load()
        {
            if (IsLoaded) return;

            try
            {
                song = LibraryIO.LoadObject<Song>(currentSongFileName);

                LoadPositionPercent();
            }
            catch { }

            loaded = true;
        }

        private void SaveSong()
        {
            Song currentSong = Library.Current.CurrentPlaylist.CurrentSong;

            if ((song != null && song.Path == currentSong.Path) || currentSong.IsEmptyOrLoading) return;

            song = Library.Current.CurrentPlaylist.CurrentSong;
            LibraryIO.SaveObject(song, currentSongFileName);
        }

        private void LoadPositionPercent()
        {
            try
            {
                string text = LibraryIO.LoadText(currentSongMillisecondsFileName);

                positionPercent = double.Parse(text);

                if (positionPercent > 1) positionPercent = positionPercent / song.NaturalDurationMilliseconds;
            }
            catch { }
        }

        public async Task SaveAsync()
        {
            Save();
        }

        public void Save()
        {
            if (!Library.IsLoaded) return;

            lock (this)
            {
                double percent = BackgroundMediaPlayer.Current.Position.TotalMilliseconds;

                if (percent == 0 || percent == positionPercent) return;

                positionPercent = percent;

                LibraryIO.SaveText(currentSongMillisecondsFileName, percent.ToString());
                SaveSong();

                //  FolderMusicDebug.SaveTextClass.Current.SaveText("CurrentSongSave", song, percent);
            }
        }
    }
}
