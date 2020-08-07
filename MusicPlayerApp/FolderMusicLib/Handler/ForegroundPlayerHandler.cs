using System;
using System.ComponentModel;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using MusicPlayer.Communication;
using MusicPlayer.Models;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;
using MusicPlayer.Models.Background;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System.Threading.Tasks;
using Windows.Foundation;

namespace MusicPlayer.Handler
{
    public class ForegroundPlayerHandler : INotifyPropertyChanged
    {
        private bool isStarted, isPlaying, isUpdatingUiPositionRatio;
        private int backgroundPlayerStateChangedCount;
        private double positionRatio;
        private TimeSpan duration;
        private MediaPlayerState currentPlayerState;
        private Song? currentSong;
        private IPlaylist currentPlaylist;
        private readonly DispatcherTimer timer;

        public bool IsPlaying
        {
            get { return isPlaying; }
            private set
            {
                if (value == isPlaying) return;

                isPlaying = value;
                OnPropertyChanged(nameof(IsPlaying));
            }
        }

        public double PositionRatio
        {
            get { return positionRatio; }
            set
            {
                if (value == positionRatio) return;

                positionRatio = value;
                OnPropertyChanged(nameof(PositionRatio));

                if (!isUpdatingUiPositionRatio && isStarted)
                {
                    TimeSpan position = BackgroundMediaPlayer.Current.NaturalDuration.Multiply(value);
                    ForegroundCommunicator.SeekPosition(position);
                }
            }
        }

        public TimeSpan Duration
        {
            get { return duration; }
            set
            {
                if (value == duration) return;

                duration = value;
                OnPropertyChanged(nameof(Duration));
            }
        }

        public MediaPlayerState CurrentPlayerState
        {
            get { return currentPlayerState; }
            set
            {
                if (value == currentPlayerState) return;

                currentPlayerState = value;
                OnPropertyChanged(nameof(CurrentPlayerState));
            }
        }

        public Song? CurrentSong
        {
            get { return currentSong; }
            set
            {
                if (Equals(value, currentSong)) return;
                currentSong = value;
                OnPropertyChanged(nameof(CurrentSong));

                CurrentPlayerState = MediaPlayerState.Closed;
                SetPostionAndDurationToView(TimeSpan.Zero, currentSong?.Duration ?? TimeSpan.Zero);

                if (isStarted) ForegroundCommunicator.SetSong(value, TimeSpan.Zero);
                if (value.HasValue) CurrentPlaylist.CurrentSong = value.Value;
            }
        }

        public IPlaylist CurrentPlaylist
        {
            get { return currentPlaylist; }
            private set
            {
                if (value == currentPlaylist) return;

                if (currentPlaylist != null) currentPlaylist.CurrentSongChanged -= CurrentPlaylist_CurrentSongChanged;
                currentPlaylist = value;
                if (currentPlaylist != null) currentPlaylist.CurrentSongChanged += CurrentPlaylist_CurrentSongChanged;
                OnPropertyChanged(nameof(CurrentPlaylist));
            }
        }

        public ILibrary Library { get; }

        public ForegroundPlayerHandler(ILibrary library)
        {
            Library = library;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(300);
            timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            CurrentPlaylist = Library.CurrentPlaylist;
            CurrentSong = CurrentPlaySong.Current.Song;

            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            IsPlaying = BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing;

            Library.CurrentPlaylistChanged += Library_CurrentPlaylistChanged;

            ForegroundCommunicator.IsPlayingChanged += ForegroundCommunicator_IsPlayingChanged;
            ForegroundCommunicator.CurrentSongChanged += ForegroundCommunicator_CurrentSongChanged;
            ForegroundCommunicator.Start(Library);

            timer.Start();
            isStarted = true;
        }

        public void Stop()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged -= BackgroundMediaPlayer_CurrentStateChanged;
            Library.CurrentPlaylistChanged -= Library_CurrentPlaylistChanged;

            ForegroundCommunicator.Stop();
            ForegroundCommunicator.IsPlayingChanged -= ForegroundCommunicator_IsPlayingChanged;
            ForegroundCommunicator.CurrentSongChanged -= ForegroundCommunicator_CurrentSongChanged;

            timer.Stop();
            isStarted = false;
        }

        private void Timer_Tick(object sender, object e)
        {
            try
            {
                TimeSpan position = BackgroundMediaPlayer.Current.Position;
                TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;
                if (duration <= TimeSpan.Zero) return;

                SetPostionAndDurationToView(position, duration);
            }
            catch (Exception exc)
            {
                MobileDebug.Service.WriteEvent("ForegroundPlayerHandler_TickError", exc);
            }
        }

        private void SetPostionAndDurationToView(TimeSpan position, TimeSpan duration)
        {
            try
            {
                isUpdatingUiPositionRatio = true;

                PositionRatio = duration > TimeSpan.Zero ? position.TotalDays / duration.TotalDays : 0;
                Duration = duration;
            }
            finally
            {
                isUpdatingUiPositionRatio = false;
            }
        }

        private async void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            MobileDebug.Service.WriteEvent("ForeHandler_CurrentStateChanged", sender.CurrentState);

            IAsyncAction setIsPlayingTask;
            IAsyncAction setCurrentPlayerStateTask = CoreApplication.MainView.CoreWindow.Dispatcher
                .RunAsync(CoreDispatcherPriority.Normal, () => CurrentPlayerState = sender.CurrentState);

            bool setIsPlaying;
            if (sender.CurrentState != MediaPlayerState.Playing)
            {
                int currentCount = ++backgroundPlayerStateChangedCount;
                await Task.Delay(1000);

                setIsPlaying = currentCount == backgroundPlayerStateChangedCount;
            }
            else setIsPlaying = true;

            if (setIsPlaying)
            {
                setIsPlayingTask = CoreApplication.MainView.CoreWindow.Dispatcher
                    .RunAsync(CoreDispatcherPriority.Normal, () => IsPlaying = sender.CurrentState == MediaPlayerState.Playing);
            }
            else setIsPlayingTask = null;

            try
            {
                await setCurrentPlayerStateTask;
                if (setIsPlayingTask != null) await setIsPlayingTask;
            }
            catch (Exception exc)
            {
                MobileDebug.Service.WriteEvent("ForeHandler_StateChangedError", exc);
            }
        }

        private void Library_CurrentPlaylistChanged(object sender, ChangedEventArgs<IPlaylist> e)
        {
            if (e.OldValue != null && BackgroundMediaPlayer.Current.NaturalDuration > TimeSpan.Zero)
            {
                e.OldValue.Position = BackgroundMediaPlayer.Current.Position;
            }

            CurrentPlaylist = e.NewValue;

            if (e.NewValue != null && e.NewValue.CurrentSong.Duration > TimeSpan.Zero)
            {
                CurrentPlayerState = MediaPlayerState.Closed;
                PositionRatio = e.NewValue.Position.TotalDays / e.NewValue.CurrentSong.Duration.TotalDays;
                Duration = e.NewValue.CurrentSong.Duration;
            }
        }

        private void CurrentPlaylist_CurrentSongChanged(object sender, ChangedEventArgs<Song> e)
        {
            IPlaylist playlist = (IPlaylist)sender;
            ForegroundCommunicator.SetSong(playlist.CurrentSong, playlist.Position);
        }

        private void ForegroundCommunicator_IsPlayingChanged(ChangedEventArgs<bool> e)
        {
            IsPlaying = e.NewValue;
        }

        private void ForegroundCommunicator_CurrentSongChanged(CurrentSongChangedEventArgs e)
        {
            CurrentSong = e.NewSong;
        }

        public void Play()
        {
            ForegroundCommunicator.Play();
        }

        public void Pause()
        {
            ForegroundCommunicator.Pause();
        }

        public void Next()
        {
            ForegroundCommunicator.Next();
        }

        public void Previous()
        {
            ForegroundCommunicator.Previous();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
