using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using MusicPlayer.Communication;
using MusicPlayer.Models;
using MusicPlayer.Models.EventArgs;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System.Threading.Tasks;
using Windows.Foundation;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.Foreground.Interfaces;
using MusicPlayer.Models.Foreground.Shuffle;

namespace MusicPlayer.Handler
{
    public class ForegroundPlayerHandler : INotifyPropertyChanged
    {
        private bool isStarting, isStarted, isPlaying, isUpdatingUiPositionRatio, isSettingCurrentSong;
        private int backgroundPlayerStateChangedCount;
        private double positionRatio;
        private TimeSpan duration, oldDuration;
        private MediaPlayerState currentPlayerState;
        private Song? currentSong;
        private IPlaylist currentPlaylist;
        private readonly DispatcherTimer timer;
        private ForegroundCommunicator communicator;
        private CancelOperationToken startToken;

        public bool IsStarting
        {
            get { return isStarting; }
            private set
            {
                if (value == isStarting) return;

                isStarting = value;
                OnPropertyChanged(nameof(IsStarting));
            }
        }

        public bool IsStarted
        {
            get { return isStarted; }
            private set
            {
                if (value == isStarted) return;

                isStarted = value;
                OnPropertyChanged(nameof(IsStarted));
            }
        }

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

                if (isUpdatingUiPositionRatio) return;

                communicator.SeekPosition(BackgroundMediaPlayer.Current.NaturalDuration.Multiply(value));
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

                try
                {
                    isSettingCurrentSong = true;

                    SendCurrentSong(value, TimeSpan.Zero);
                    SetPositionAndDurationToView(TimeSpan.Zero, currentSong?.Duration ?? TimeSpan.Zero);

                    if (value.HasValue) CurrentPlaylist.CurrentSong = value.Value;
                }
                finally
                {
                    isSettingCurrentSong = false;
                }
            }
        }

        public IPlaylist CurrentPlaylist
        {
            get { return currentPlaylist; }
            private set
            {
                if (value == currentPlaylist) return;

                Unsubscribe(currentPlaylist);
                currentPlaylist = value;
                Subscribe(currentPlaylist);

                OnPropertyChanged(nameof(CurrentPlaylist));
                communicator.SendPlaylist(currentPlaylist);
            }
        }

        public ILibrary Library { get; }

        public ForegroundPlayerHandler(ILibrary library)
        {
            IsStarting = true;
            IsStarted = false;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(300);
            timer.Tick += Timer_Tick;
            communicator = new ForegroundCommunicator();
            Library = library;
            CurrentPlaylist = Library.CurrentPlaylist;
        }

        public async Task Start()
        {
            if (startToken != null) return;
            try
            {
                startToken = new CancelOperationToken();
                IsStarting = true;

                bool isSynced = IsSynced(Library.CurrentPlaylist, CurrentPlaylistStore.Current.CurrentSong,
                    CurrentPlaylistStore.Current.SongsHash);

                if (isSynced)
                {
                    CurrentPlaylist = Library.CurrentPlaylist;
                    CurrentSong = CurrentPlaylistStore.Current.CurrentSong;

                    if (CurrentPlaylist != null) CurrentPlaylist.Loop = CurrentPlaylistStore.Current.Loop;
                }
                else
                {
                    CurrentPlaylist = Library.CurrentPlaylist;
                    CurrentSong = CurrentPlaylist?.CurrentSong;
                }

                Library.CurrentPlaylistChanged += Library_CurrentPlaylistChanged;
                Subscribe(Library.CurrentPlaylist);

                communicator.IsPlayingReceived += Communicator_IsPlayingReceived;
                communicator.CurrentSongReceived += Communicator_CurrentSongReceived;
                await communicator.Start(startToken);

                if (startToken.IsCanceled)
                {
                    Stop();
                    return;
                }

                if (!isSynced) communicator.SendPlaylist(CurrentPlaylist);

                BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
                CurrentPlayerState = BackgroundMediaPlayer.Current.CurrentState;
                IsPlaying = BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing;

                timer.Start();
                IsStarted = true;
                startToken.Complete();
            }
            finally
            {
                IsStarting = false;
            }
        }

        private static bool IsSynced(IPlaylist currentPlaylist, Song? currentSong, string songsHash)
        {
            if (currentSong.HasValue ^ currentPlaylist != null) return false;

            IEnumerable<Song> songs = (IEnumerable<Song>)currentPlaylist?.Songs.Shuffle ?? new Song[0];
            if (songs.All(s => s.FullPath != currentSong?.FullPath)) return false;

            return songsHash == Utils.GetSha256Hash(songs);
        }

        public void Stop()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged -= BackgroundMediaPlayer_CurrentStateChanged;

            communicator.Stop();
            communicator.IsPlayingReceived -= Communicator_IsPlayingReceived;
            communicator.CurrentSongReceived -= Communicator_CurrentSongReceived;

            Library.CurrentPlaylistChanged -= Library_CurrentPlaylistChanged;
            Unsubscribe(Library.CurrentPlaylist);

            timer.Stop();

            startToken?.Cancel();
            startToken = null;
            IsStarted = false;
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += Playlist_CurrentSongChanged;
            playlist.PlaybackRateChanged += Playlist_PlaybackRateChanged;
            playlist.LoopChanged += Playlist_LoopChanged;
            Subscribe(playlist.Songs);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= Playlist_CurrentSongChanged;
            playlist.LoopChanged -= Playlist_LoopChanged;
            Unsubscribe(playlist.Songs);
        }

        private void Subscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.ShuffleChanged += Songs_ShuffleChanged;
            Subscribe(songs.Shuffle);
        }

        private void Unsubscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.ShuffleChanged += Songs_ShuffleChanged;
            Unsubscribe(songs.Shuffle);
        }

        private void Subscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed += Shuffle_Changed;
        }

        private void Unsubscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed -= Shuffle_Changed;
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
                SetPositionAndDurationToView(e.NewValue.Position, e.NewValue.CurrentSong.Duration);
            }
        }

        private void Playlist_CurrentSongChanged(object sender, ChangedEventArgs<Song> e)
        {
            if (isSettingCurrentSong) return;

            MobileDebug.Service.WriteEvent("ForeHandlerPlaylist_CurrentSongChanged", CurrentPlaylist.CurrentSong, CurrentPlaylist.Position);
            SendCurrentSong(CurrentPlaylist.CurrentSong, TimeSpan.Zero);
        }

        private void Playlist_PlaybackRateChanged(object sender, ChangedEventArgs<double> e)
        {
            communicator.SendPlaybackRate(e.NewValue);
        }

        private void Playlist_LoopChanged(object sender, ChangedEventArgs<LoopType> e)
        {
            communicator.SendLoop(e.NewValue);
        }

        private void Songs_ShuffleChanged(object sender, ShuffleChangedEventArgs e)
        {
            communicator.SendSongs(e.NewShuffleSongs.ToArray());
        }

        private void Shuffle_Changed(object sender, ShuffleCollectionChangedEventArgs e)
        {
            IShuffleCollection shuffle = (IShuffleCollection)sender;
            communicator.SendSongs(shuffle.ToArray());
        }

        private void Timer_Tick(object sender, object e)
        {
            try
            {
                TimeSpan position = BackgroundMediaPlayer.Current.Position;
                TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;
                if (duration == oldDuration || (duration <= TimeSpan.Zero && CurrentSong != null)) return;

                SetPositionAndDurationToView(position, duration);
            }
            catch (Exception exc)
            {
                MobileDebug.Service.WriteEvent("ForegroundPlayerHandler_TickError", exc);
            }
        }

        private void SetPositionAndDurationToView(TimeSpan position, TimeSpan duration)
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
                    .RunAsync(CoreDispatcherPriority.Normal,
                        () => IsPlaying = sender.CurrentState == MediaPlayerState.Playing);
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

        private void Communicator_IsPlayingReceived(object sender, bool e)
        {
            IsPlaying = e;
        }

        private void Communicator_CurrentSongReceived(object sender, string e)
        {
            Song newCurrentSong;
            if (!string.IsNullOrWhiteSpace(e) && CurrentPlaylist != null &&
                CurrentPlaylist.Songs.TryGetSong(e, out newCurrentSong)) CurrentSong = newCurrentSong;
            else CurrentSong = null;
        }

        private void SendCurrentSong(Song? song, TimeSpan position)
        {
            oldDuration = CurrentSong?.Duration == Duration ? TimeSpan.Zero : Duration;
            communicator.SendCurrentSong(song, TimeSpan.Zero);
        }

        public void Play()
        {
            communicator.Play();
        }

        public void Pause()
        {
            communicator.Pause();
        }

        public void Next()
        {
            communicator.Next();
        }

        public void Previous()
        {
            communicator.Previous();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
