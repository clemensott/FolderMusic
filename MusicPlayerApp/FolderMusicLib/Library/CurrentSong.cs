using System.Threading.Tasks;
using Windows.Media.Playback;

namespace LibraryLib
{
    public class CurrentSong
    {
        private static CurrentSong instance;

        public static CurrentSong Current
        {
            get
            {
                if (instance == null) instance = new CurrentSong();

                return instance;
            }
        }

        private readonly string currentSongMillisecondsFileName = "CurrentSongMilliseconds.txt",
            currentSongFileName = "CurrentSong.xml";

        private bool loaded;
        private double positionMilliseconds = 0;
        private Song song;

        public bool IsLoaded { get { return loaded; } }

        public double PositionMilliseconds { get { return positionMilliseconds; } }

        public Song Song { get { return song != null ? song : new Song(); } }

        private CurrentSong() { }

        public void Unset()
        {
            song = null;
            positionMilliseconds = 0;

            LibraryIO.Delete(currentSongFileName);
            LibraryIO.Delete(currentSongMillisecondsFileName);

            loaded = false;
        }

        public async Task Load()
        {
            if (IsLoaded) return;

            try
            {
                song = await LibraryIO.LoadObject<Song>(currentSongFileName);

                await LoadPositionMilliseconds();
            }
            catch { }

            loaded = true;
        }

        private async Task SaveSong()
        {
            if (!Library.IsLoaded) return;

            song = Library.Current.CurrentPlaylist.CurrentSong;
            await LibraryIO.SaveObject(song, currentSongFileName);
        }

        private async Task LoadPositionMilliseconds()
        {
            try
            {
                string text = await LibraryIO.LoadText(currentSongMillisecondsFileName);

                positionMilliseconds = double.Parse(text);
            }
            catch { }
        }

        public async Task Save()
        {
            if (!Library.IsLoaded) return;

            double milliseconds = BackgroundMediaPlayer.Current.Position.TotalMilliseconds;

            await LibraryIO.SaveText(milliseconds.ToString(), currentSongMillisecondsFileName);
            await SaveSong();
        }
    }
}
