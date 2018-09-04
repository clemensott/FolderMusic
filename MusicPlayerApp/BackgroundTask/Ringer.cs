using LibraryLib;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;

namespace BackgroundTask
{
    class Ringer : IDisposable
    {
        private const int millisAroundRing = 5000;
        private static Ringer instance;

        private static readonly string dataPath = ApplicationData.Current.LocalFolder.Path + "\\Times.txt";

        private bool isOn, isDisposed;
        private int periodSpanMillis;
        private string ringFilePath;
        private Timer timer;

        public static Ringer Current
        {
            get
            {
                if (instance == null) instance = new Ringer();

                return instance;
            }
        }

        public bool IsRinging { get; private set; }

        public bool WillRing { get; private set; }

        private Ringer()
        {
            timer = new Timer(Ring, null, Timeout.Infinite, Timeout.Infinite);

            ReloadTimes();

            BackgroundMediaPlayer.Current.MediaOpened += Current_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed += Current_MediaFailed;
            BackgroundMediaPlayer.Current.MediaEnded += Current_MediaEnded;
        }

        private async void Current_MediaOpened(MediaPlayer sender, object args)
        {
            if (!IsRinging) return;

            sender.Volume = 1;

            do
            {
                sender.Play();
                await Task.Delay(1000);

                FolderMusicDebug.SaveTextClass.Current.SaveText("OpenRing", sender.CurrentState);
            }
            while (sender.CurrentState != MediaPlayerState.Playing);
        }

        public static void Create()
        {
            Ringer ringer = Current;
        }

        private void Current_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            if (!IsRinging) return;

            IsRinging = false;
        }

        private void Current_MediaEnded(MediaPlayer sender, object args)
        {
            if (WillRing) SetRingFile();
            else if (IsRinging)
            {
                IsRinging = false;

                BackgroundAudioTask.Current.SetCurrentSong(true);
            }
        }

        public async void ReloadTimes()
        {
            int startMillis;

            try
            {
                var ringerData = await PathIO.ReadLinesAsync(dataPath);

                isOn = bool.Parse(ringerData[0]);
                periodSpanMillis = int.Parse(ringerData[1]) * 60000;
                ringFilePath = ringerData[1];

                startMillis = GetStartTimeMilliseconds();

            }
            catch
            {
                startMillis = periodSpanMillis = Timeout.Infinite;
            }

            if (!isOn) startMillis = Timeout.Infinite;

            IsRinging = false;
            timer.Change(startMillis, periodSpanMillis);

            FolderMusicDebug.SaveTextClass.Current.SaveText("RingerSet", "Start [ms]: " +
               startMillis, "Periode [ms]: " + periodSpanMillis);
        }

        private int GetStartTimeMilliseconds()
        {
            double millisecondsThisHour = (DateTime.Now.TimeOfDay.TotalMilliseconds - millisAroundRing) %
                TimeSpan.FromHours(1).TotalMilliseconds;

            double probertlyStartMillis = (periodSpanMillis - millisecondsThisHour) % periodSpanMillis;

            return Convert.ToInt32(periodSpanMillis + probertlyStartMillis) % periodSpanMillis;
        }

        private async void Ring(object obj)
        {
            WillRing = false;
            MediaPlayerState state = BackgroundMediaPlayer.Current.CurrentState;

            FolderMusicDebug.SaveTextClass.Current.SaveText("Ring");

            if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing)
            {
                await Task.Delay(1000);

                if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Playing) return;
            }

            WillRing = true;

            await Task.Delay(millisAroundRing);

            if (!WillRing) return;

            double millisUntilCurrentSongEnds = BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds -
                BackgroundMediaPlayer.Current.Position.TotalMilliseconds;

            if (millisUntilCurrentSongEnds < millisAroundRing) return;

            SetRingFile();
        }

        private async void SetRingFile()
        {
            StorageFile file;
            WillRing = false;

            try
            {
                FolderMusicDebug.SaveTextClass.Current.SaveText("RingFileGet");

                Uri uri = new Uri("ms-appx:///Assets/Glockenschlag.mp3");
                file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            }
            catch
            {
                FolderMusicDebug.SaveTextClass.Current.SaveText("RingFileFail");
                return;
            }

            BackgroundAudioTask.Current.Pause();

            IsRinging = true;

            TimeSpan position = BackgroundMediaPlayer.Current.Position;
            TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;
            Library.Current.CurrentPlaylist.SongPositionPercent = position.TotalMilliseconds / duration.TotalMilliseconds;

            BackgroundMediaPlayer.Current.AutoPlay = true;
            BackgroundMediaPlayer.Current.SetFileSource(file);
        }

        public void SetTimesIfIsDisposed()
        {
            if (!isOn || !isDisposed || periodSpanMillis == Timeout.Infinite) return;

            int startMillis = GetStartTimeMilliseconds();

            timer.Change(startMillis, periodSpanMillis);
        }

        public void Dispose()
        {
            isDisposed = true;
            timer.Dispose();
        }
    }
}
