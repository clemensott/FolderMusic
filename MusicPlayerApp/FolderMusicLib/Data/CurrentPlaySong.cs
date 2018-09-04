using System.Threading.Tasks;
using Windows.Media.Playback;

namespace MusicPlayer.Data
{
    public class CurrentPlaySong
    {
        private const string currentSongMillisecondsFileName = "CurrentSongMilliseconds.txt",
            currentSongFileName = "CurrentSong.xml";

        private static CurrentPlaySong instance;

        public static CurrentPlaySong Current
        {
            get
            {
                if (instance == null) instance = new CurrentPlaySong();

                return instance;
            }
        }

        private bool loaded;
        private double positionPercent = 0;
        private Song song;

        public bool IsLoaded { get { return loaded; } }

        public double PositionPercent { get { return positionPercent; } }

        public Song Song { get { return song != null ? song : new Song(); } }

        private CurrentPlaySong() { }

        public void Unset()
        {
            loaded = false;
            song = null;
            positionPercent = 0;

            IO.Delete(currentSongFileName);
            IO.Delete(currentSongMillisecondsFileName);
        }

        public void Load()
        {
            if (IsLoaded) return;

            try
            {
                song = IO.LoadObject<Song>(currentSongFileName);

                if (song.NaturalDurationMilliseconds == 1) song.LoadNaturalDuration().Wait();

                LoadPositionPercent();
            }
            catch { }

            loaded = true;
        }

        private void SaveSong()
        {
            if (song.Path == Library.Current.CurrentPlaylist.CurrentSong.Path) return;

            song = Library.Current.CurrentPlaylist.CurrentSong;

            IO.SaveObject(currentSongFileName, song);
        }

        private void LoadPositionPercent()
        {
            try
            {
                string text = IO.LoadText(currentSongMillisecondsFileName);

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
            if (!Library.IsLoaded()) return;

            lock (this)
            {
                double percent = BackgroundMediaPlayer.Current.Position.TotalMilliseconds /
                    BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds;

                if (percent == 0 || percent == positionPercent) return;

                positionPercent = percent;

                IO.SaveText(currentSongMillisecondsFileName, percent.ToString());
                SaveSong();

                FolderMusicDebug.DebugEvent.SaveText("CurrentSongSave", song, percent);
            }
        }
    }
}
