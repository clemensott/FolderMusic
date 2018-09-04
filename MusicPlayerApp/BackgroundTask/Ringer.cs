using MusicPlayer.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;

namespace BackgroundTask
{
    enum RingerState { Idle, Waiting, Ringing, Disposed }

    class Ringer : IBackgroundPlayer, IDisposable
    {
        private const int millisAroundRing = 5000;

        private static readonly string dataPath = ApplicationData.Current.LocalFolder.Path + "\\Times.txt";

        private bool isOn, isDisposed;
        private int periodSpanMillis;
        private string ringFilePath;
        private Timer timer;
        private BackgroundAudioTask task;

        public RingerState State { get; private set; }

        public Ringer(BackgroundAudioTask backgroundAudioTask)
        {
            task = backgroundAudioTask;
            timer = new Timer(Ring, null, Timeout.Infinite, Timeout.Infinite);

            ReloadTimes();
        }

        public void Play()
        {
            Library.Current.IsPlaying = true;
        }

        public void Pause()
        {
            Library.Current.IsPlaying = false;
        }

        public void Next(bool fromEnded)
        {

        }

        public void Previous()
        {

        }

        public void SetCurrent()
        {
            if (!Library.Current.IsPlaying || State == RingerState.Ringing) return;
            task.PlayerType = BackgroundPlayerType.Ringer;

            try
            {
                FolderMusicDebug.DebugEvent.SaveText("RingFileGet");

                Uri uri = new Uri("ms-appx:///Assets/Glockenschlag.mp3");
                StorageFile file = GetRingerFile(uri);

                try
                {
                    State = RingerState.Ringing;

                    BackgroundMediaPlayer.Current.Volume = 1;
                    BackgroundMediaPlayer.Current.AutoPlay = true;
                    BackgroundMediaPlayer.Current.SetFileSource(file);
                }
                catch
                {
                    task.PlayerType = BackgroundPlayerType.Music;
                    task.BackgroundPlayer.SetCurrent();
                }
            }
            catch
            {
                FolderMusicDebug.DebugEvent.SaveText("RingFileFail");
            }
        }

        private StorageFile GetRingerFile(Uri uri)
        {
            Task<StorageFile> task = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask();
            task.Wait();

            return task.Result;
        }

        public void MediaOpened(MediaPlayer sender, object args)
        {
            State = RingerState.Ringing;

            //sender.Volume = 1;

            //do
            //{
            //    sender.Play();
            //    Task.Delay(1000).Wait();

            //    FolderMusicDebug.DebugEvent.SaveText("OpenRing", sender.CurrentState);
            //}
            //while (sender.CurrentState != MediaPlayerState.Playing);
        }

        public void MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            State = RingerState.Idle;

            task.PlayerType = BackgroundPlayerType.Music;
            task.BackgroundPlayer.SetCurrent();
        }

        public void MediaEnded(MediaPlayer sender, object args)
        {
            FolderMusicDebug.DebugEvent.SaveText("RingerEnded", "RingerState: " + State);

            if (State == RingerState.Waiting) SetCurrent();
            else if (State == RingerState.Ringing)
            {
                task.PlayerType = BackgroundPlayerType.Music;
                task.BackgroundPlayer.SetCurrent();
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

            timer.Change(startMillis, periodSpanMillis);

            FolderMusicDebug.DebugEvent.SaveText("RingerSet", "Start [ms]: " +
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
            FolderMusicDebug.DebugEvent.SaveText("Ring");

            if (!Library.Current.IsPlaying) return;

            State = RingerState.Waiting;
            await Task.Delay(millisAroundRing);

            if (State != RingerState.Waiting) return;

            double millisUntilCurrentSongEnds = BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds -
                BackgroundMediaPlayer.Current.Position.TotalMilliseconds;

            if (millisUntilCurrentSongEnds < millisAroundRing) return;

            SetCurrent();
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
