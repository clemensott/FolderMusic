using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using MusicPlayer.Communication;
using MusicPlayer.Models;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;

namespace MusicPlayer.Handler
{
    public class BackgroundPlayerHandler
    {
        private const int maxFailOrSetCount = 15;

        private bool isPlaying, playNext = true, mediaEndedHappened;
        private int failedCount, setSongCount;
        private LoopType loop;
        private Song openSong;
        private Song? currentSong;
        private Song[] songs;
        private DateTime setPositionTime;
        private TimeSpan setPositionPosition;
        private readonly BackgroundCommunicator communicator;
        private readonly SystemMediaTransportControls smtc;

        public event EventHandler<ChangedEventArgs<Song?>> CurrentSongChanged;
        public event EventHandler<ChangedEventArgs<Song[]>> SongsChanged;
        public event EventHandler<ChangedEventArgs<LoopType>> LoopChanged;

        public bool IsPlaying
        {
            get { return isPlaying; }
            set
            {
                if (value == isPlaying) return;

                isPlaying = value;
                communicator.SendIsPlaying(isPlaying);
            }
        }

        public TimeSpan Position { get; private set; }

        public Song? CurrentSong
        {
            get { return currentSong; }
            private set
            {
                if (Equals(value, currentSong)) return;

                ChangedEventArgs<Song?> args = new ChangedEventArgs<Song?>(currentSong, value);
                currentSong = value;
                CurrentSongChanged?.Invoke(this, args);

                communicator.SendCurrentSong(currentSong);
            }
        }

        public Song[] Songs
        {
            get { return songs; }
            private set
            {
                if (value == null || value.BothNullOrSequenceEqual(songs)) return;

                ChangedEventArgs<Song[]> args = new ChangedEventArgs<Song[]>(songs, value);
                songs = value;
                SongsChanged?.Invoke(this, args);
            }
        }

        public LoopType Loop
        {
            get { return loop; }
            set
            {
                if (value == loop) return;

                ChangedEventArgs<LoopType> args = new ChangedEventArgs<LoopType>(loop, value);
                loop = value;
                LoopChanged?.Invoke(this, args);

                BackgroundMediaPlayer.Current.IsLoopingEnabled = Loop == LoopType.Current;
            }
        }

        public BackgroundPlayerHandler(Song? currentSong,
            TimeSpan position, LoopType loop, Song[] songs)
        {
            communicator = new BackgroundCommunicator();

            smtc = SystemMediaTransportControls.GetForCurrentView();
            smtc.ButtonPressed += MediaTransportControlButtonPressed;

            CurrentSong = currentSong;
            Position = position;
            Loop = loop;
            Songs = songs ?? new Song[0];
        }

        public async Task Start()
        {
            ActivateSystemMediaTransportControl();
            SetNextSongIfMediaEndedNotHappens();

            communicator.CurrentSongReceived += Communicator_CurrentSongReceived;
            communicator.PositionReceived += Communicator_PositionReceived;
            communicator.PlaylistReceived += Communicator_PlaylistReceived;
            communicator.SongsReceived += Communicator_SongsReceived;
            communicator.LoopReceived += Communicator_LoopReceived;
            communicator.PlayReceived += Communicator_PlayReceived;
            communicator.PauseReceived += Communicator_PauseReceived;
            communicator.NextReceived += Communicator_NextReceived;
            communicator.PreviousReceived += Communicator_PreviousReceived;

            BackgroundMediaPlayer.Current.MediaEnded += BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened += BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed += BackgroundMediaPlayer_MediaFailed;
            
            communicator.Start();

            await SetSong(CurrentSong, Position);
        }

        public void Stop()
        {
            Pause();

            mediaEndedHappened = true;

            communicator.CurrentSongReceived -= Communicator_CurrentSongReceived;
            communicator.PositionReceived -= Communicator_PositionReceived;
            communicator.PlaylistReceived -= Communicator_PlaylistReceived;
            communicator.SongsReceived -= Communicator_SongsReceived;
            communicator.LoopReceived -= Communicator_LoopReceived;
            communicator.PlayReceived -= Communicator_PlayReceived;
            communicator.PauseReceived -= Communicator_PauseReceived;
            communicator.NextReceived -= Communicator_NextReceived;
            communicator.PreviousReceived -= Communicator_PreviousReceived;

            BackgroundMediaPlayer.Current.MediaEnded -= BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened -= BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed -= BackgroundMediaPlayer_MediaFailed;
            
            communicator.Stop();
        }

        private void ActivateSystemMediaTransportControl()
        {
            smtc.IsEnabled = smtc.IsPauseEnabled = smtc.IsPlayEnabled =
                smtc.IsRewindEnabled = smtc.IsPreviousEnabled = smtc.IsNextEnabled = true;
        }

        // A workaround because sometimes Media_Ended does not get triggered the first time
        private async void SetNextSongIfMediaEndedNotHappens()
        {
            while (!mediaEndedHappened)
            {
                TimeSpan position = BackgroundMediaPlayer.Current.Position;
                TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;

                if (duration > TimeSpan.Zero && position >= duration && !BackgroundMediaPlayer.Current.IsLoopingEnabled)
                {
                    await Task.Delay(5000);

                    if (mediaEndedHappened) break;

                    MobileDebug.Service.WriteEvent("SetNextSongIfMediaEndedNotHappens3");
                    await Next(true);
                }

                await Task.Delay(1000);
            }
        }

        private async void Communicator_CurrentSongReceived(object sender, CurrentSongReceivedEventArgs e)
        {
            await SetSong(e.NewSong, e.Position);
        }

        private void Communicator_PositionReceived(object sender, TimeSpan e)
        {
            SeekToPosition(e);
        }

        private async void Communicator_PlaylistReceived(object sender, PlaylistReceivedEventArgs e)
        {
            Loop = e.Loop;
            Songs = e.Songs;
            await SetSong(e.CurrentSong, e.Position);
        }

        private void Communicator_SongsReceived(object sender, Song[] e)
        {
            Songs = e;
            
            Song newCurrentSong;
            string currentSongPath = CurrentSong?.FullPath;
            if (!Songs.TryGetSong(currentSongPath, out newCurrentSong)) return;
            
            currentSong = newCurrentSong;
            UpdateSystemMediaTransportControl();
        }

        private void Communicator_LoopReceived(object sender, LoopType e)
        {
            Loop = e;
        }

        private async void Communicator_PlayReceived(object sender, EventArgs e)
        {
            await Play();
        }

        private void Communicator_PauseReceived(object sender, EventArgs e)
        {
            Pause();
        }

        private async void Communicator_NextReceived(object sender, EventArgs e)
        {
            await Next(false);
        }

        private async void Communicator_PreviousReceived(object sender, EventArgs e)
        {
            await Previous();
        }

        private async void MediaTransportControlButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            MobileDebug.Service.WriteEventPair("MTCPressed",
                "Button", args.Button, "Song", openSong);

            await MediaTransportControlButtonPressed(args.Button);
        }

        private async Task MediaTransportControlButtonPressed(SystemMediaTransportControlsButton button)
        {
            switch (button)
            {
                case SystemMediaTransportControlsButton.Play:
                    await Play();
                    return;

                case SystemMediaTransportControlsButton.Pause:
                    Pause();
                    return;

                case SystemMediaTransportControlsButton.Previous:
                    await Previous();
                    return;

                case SystemMediaTransportControlsButton.Next:
                    await Next(false);
                    return;

                case SystemMediaTransportControlsButton.Rewind:
                    SeekToPosition(TimeSpan.Zero);
                    return;
            }
        }

        private void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            MobileDebug.Service.WriteEventPair("Open", "SetSongCount", setSongCount,
                "Sender.State", sender.CurrentState, "IsPlaying", IsPlaying,
                "Pos", Position.TotalSeconds, "CurrentSongFileName", CurrentSong);

            playNext = true;
            failedCount = 0;
            openSong = CurrentSong.GetValueOrDefault();

            setPositionTime = DateTime.Now;
            setPositionPosition = Position;
            if (setPositionPosition > TimeSpan.Zero) sender.Position = setPositionPosition;

            if (IsPlaying)
            {
                if (sender.Position > TimeSpan.Zero) Volume0To1();
                else BackgroundMediaPlayer.Current.Play();
            }
            else smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

            UpdateSystemMediaTransportControl();
        }

        private void UpdateSystemMediaTransportControl()
        {
            Song? song = CurrentSong;
            if (!song.HasValue) return;

            SystemMediaTransportControlsDisplayUpdater du = smtc.DisplayUpdater;

            if (du.Type != MediaPlaybackType.Music) du.Type = MediaPlaybackType.Music;
            if (du.MusicProperties.Title == song.Value.Title && du.MusicProperties.Artist == song.Value.Artist) return;

            du.MusicProperties.Title = song.Value.Title;
            du.MusicProperties.Artist = song.Value.Artist;
            du.Update();
        }

        private async void BackgroundMediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            MobileDebug.Service.WriteEvent("Fail", args.ExtendedErrorCode, args.Error, args.ErrorMessage);
            await Task.Delay(100);

            failedCount++;

            if (failedCount >= maxFailOrSetCount) failedCount = 0;
            else if (args.Error == MediaPlayerError.Unknown)
            {
                await Task.Delay(2000);

                await SetSong(CurrentSong, Position);
                return;
            }

            //CurrentSong.SetFailed();
            sender.Pause();
        }

        private async void BackgroundMediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            mediaEndedHappened = true;
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;

            TimeSpan durationLeft = BackgroundMediaPlayer.Current.NaturalDuration - setPositionPosition;
            bool passedEnoughTime = (DateTime.Now - setPositionTime).TotalDays > durationLeft.TotalDays / 2;

            //MobileDebug.Service.WriteEventPair("MusicEnded", "SMTC-State", smtc.PlaybackStatus,
            //    "Pos", sender.Position.TotalSeconds, "Duration", sender.NaturalDuration.TotalSeconds,
            //    "CurrentSongFileName", CurrentSong, "setPositionTime", setPositionTime,
            //    "setPositionPosition", setPositionPosition, "passedTime", passedEnoughTime);

            if (DateTime.Now - setPositionTime < TimeSpan.FromSeconds(2))
            {
                MobileDebug.Service.WriteEvent("Skipped Song bug", "CurrentSongFileName", CurrentSong,
                    "setPositionTime", setPositionTime, "setPositionPosition", setPositionPosition,
                    "passedTime", passedEnoughTime);
            }

            await Next(true);
            //if (passedEnoughTime) await Next(true);
            //else // A workaround because sometimes a song ends without even been played
            //{
            //    BackgroundMediaPlayer.Current.Position = TimeSpan.Zero;
            //    BackgroundMediaPlayer.Current.Play();
            //}
        }

        public async Task Play()
        {
            IsPlaying = true;

            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing ||
                BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Opening) return;
            if (setSongCount >= maxFailOrSetCount)
            {
                setSongCount = 0;

                Volume0To1();

                //MobileDebug.Service.WriteEvent("PlayBecauseOfSetSongCount", BackgroundMediaPlayer.Current.CurrentState);
            }
            else if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Closed ||
                     BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped)
            {
                //MobileDebug.Service.WriteEvent("PlayClosedOrStopped", BackgroundMediaPlayer.Current.CurrentState);
                await SetSong(CurrentSong, Position);
            }
            else if (BackgroundMediaPlayer.Current.NaturalDuration == TimeSpan.Zero)
            {
                //MobileDebug.Service.WriteEvent("SetOnPlayDurationZero", CurrentSong);
                await SetSong(CurrentSong, Position);
            }
            else
            {
                Volume0To1();
                setSongCount = 0;
            }

            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;

            BackgroundMediaPlayer.Current.Play();
        }

        private static async void Volume0To1()
        {
            BackgroundMediaPlayer.Current.Volume = 0;
            BackgroundMediaPlayer.Current.Play();

            const double step = 0.1;

            for (double i = step; i < 1; i += step)
            {
                BackgroundMediaPlayer.Current.Volume = Math.Sqrt(i);
                await Task.Delay(10);
            }
        }

        public async void Pause()
        {
            IsPlaying = false;

            smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

            if (BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Paused) await Volume1To0AndPause();
        }

        private static async Task Volume1To0AndPause()
        {
            const double step = 0.1;

            for (double i = 1; i > 0; i -= step)
            {
                BackgroundMediaPlayer.Current.Volume = i;
                await Task.Delay(10);
            }

            BackgroundMediaPlayer.Current.Pause();
        }

        public Task Next(bool fromEnded)
        {
            playNext = true;
            Song? newCurrentSong;
            if (!TryNext(CurrentSong, out newCurrentSong) && fromEnded) Pause();
            MobileDebug.Service.WriteEvent("BackHandler_Next", CurrentSong, newCurrentSong);
            return SetSong(newCurrentSong);
        }

        private bool TryNext(Song? song, out Song? newCurrentSong)
        {
            int index = song.HasValue ? Songs.IndexOf(song.Value) + 1 : 0;
            if (index >= Songs.Length) index = 0;

            newCurrentSong = Songs.Length > 0 ? (Song?)Songs[index] : null;
            return index > 0;
        }

        public Task Previous()
        {
            playNext = false;
            return SetSong(Previous(CurrentSong));
        }

        private Song? Previous(Song? song)
        {
            if (Songs.Length == 0) return null;

            int index = song.HasValue ? Songs.IndexOf(song.Value) : -1;
            if (index == -1) return Songs[0];
            if (index == 0) return Songs[Songs.Length - 1];

            return Songs[index - 1];
        }

        public void SeekToPosition(TimeSpan position)
        {
            Position = position;
            BackgroundMediaPlayer.Current.Position = position;

            setPositionTime = DateTime.Now;
            setPositionPosition = Position;
        }

        public Task SetSong(Song? song)
        {
            return SetSong(song, TimeSpan.Zero);
        }

        private DateTime lastSetSong;
        private int shortTimeBetweenSetSongCount = 0;

        public async Task SetSong(Song? song, TimeSpan position)
        {
            if (DateTime.Now - lastSetSong < TimeSpan.FromMilliseconds(300)) shortTimeBetweenSetSongCount++;
            else if (DateTime.Now - lastSetSong > TimeSpan.FromSeconds(5)) shortTimeBetweenSetSongCount = 0;
            lastSetSong = DateTime.Now;
            if (shortTimeBetweenSetSongCount > 20) return;

            MobileDebug.Service.WriteEventPair("TrySet", "OpenPath", openSong.FullPath,
                "CurrentSongFileName", song?.FullPath ?? "<null>",
                "Position", position, "IsOpen", Equals(song, openSong));

            CurrentSong = song;
            Position = position;

            if (!song.HasValue)
            {
                Pause();
                return;
            }

            BackgroundMediaPlayer.Current.AutoPlay = IsPlaying && position == TimeSpan.Zero;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(song.Value.FullPath);
                BackgroundMediaPlayer.Current.SetFileSource(file);
                setSongCount++;
                MobileDebug.Service.WriteEvent("Set", setSongCount, song);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("Set catch", e, CurrentSong);
                await Task.Delay(100);

                if (playNext) await Next(false);
                else await Previous();
            }
        }

        public void Dispose()
        {
            smtc.ButtonPressed -= MediaTransportControlButtonPressed;
        }
    }
}
