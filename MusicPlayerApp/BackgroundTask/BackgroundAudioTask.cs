using MusicPlayer.Data;
using MusicPlayer.Data.Loop;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;

namespace BackgroundTask
{
    enum BackgroundPlayerType { Music, Ringer }

    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        private const long smtcLastPressedMinDeltaTicks = TimeSpan.TicksPerMillisecond * 300;
        private static long smtcLastPressedTicks = 0;

        private SystemMediaTransportControls smtc;
        private BackgroundTaskDeferral deferral;
        private BackgroundPlayerType playerType;
        private MusicPlayer musicPlayer;
        private Ringer ringer;

        internal BackgroundPlayerType PlayerType
        {
            get { return playerType; }
            set { playerType = value; }
        }

        internal IBackgroundPlayer BackgroundPlayer
        {
            get
            {
                return playerType == BackgroundPlayerType.Music ? (IBackgroundPlayer)musicPlayer : ringer;
            }
        }

        //private Song CurrentSong { get { return Library.Current.CurrentPlaylist.CurrentSong; } }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //System.Diagnostics.Debug.WriteLine("BackgroundId: " + Task.CurrentId);

            FolderMusicDebug.DebugEvent.Id = taskInstance.InstanceId.ToString();
            FolderMusicDebug.DebugEvent.SaveText("Run", "Hash: " + GetHashCode());

            deferral = taskInstance.GetDeferral();

            smtcLastPressedTicks = DateTime.Now.Ticks;
            smtc = SystemMediaTransportControls.GetForCurrentView();

            musicPlayer = new MusicPlayer(this, smtc);
            ringer = new Ringer(this);

            LoadCurrentSongAndLibrary();
            SetEvents();

            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += TaskCompleted;
        }

        private void SetEvents()
        {
            smtc.ButtonPressed += MediaTransportControlButtonPressed;

            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded += BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened += BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed += BackgroundMediaPlayer_MediaFailed;

            Feedback.Current.OnCurrentPlaylistPropertyChanged += OnCurrentPlaylistPropertyChanged;
            Feedback.Current.OnCurrentSongPropertyChanged += OnCurrentSongPropertyChanged;
            Feedback.Current.OnLibraryChanged += OnLibraryChanged;
            Feedback.Current.OnLoopPropertyChanged += OnLoopPropertyChanged;
            Feedback.Current.OnPlayStateChanged += OnPlayStateChanged;
            Feedback.Current.OnPlaylistsPropertyChanged += OnPlaylistsPropertyChanged;
            Feedback.Current.OnShufflePropertyChanged += OnShufflePropertyChanged;
            Feedback.Current.OnSettingsPropertyChanged += OnSettingsPropertyChanged;
            Feedback.Current.OnSongsPropertyChanged += OnSongsPropertyChanged;
        }

        private void LoadCurrentSongAndLibrary()
        {
            LoadLibraryData();

            FolderMusicDebug.DebugEvent.SaveText("SetLoadCurrentSongAndLibrary", Library.Current.CurrentPlaylist.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        public void LoadLibraryData()
        {
            Library.Load(false);
            FolderMusicDebug.DebugEvent.SaveText("AfterLoad", Library.Current.CurrentPlaylist.CurrentSong);

            SetLoopToBackgroundPlayer();
        }

        public void SetLoopToBackgroundPlayer()
        {
            BackgroundMediaPlayer.Current.IsLoopingEnabled = Library.Current.CurrentPlaylist.Loop == LoopType.Current;
        }

        private void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            BackgroundPlayer.MediaOpened(sender, args);
        }

        private void BackgroundMediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            BackgroundPlayer.MediaFailed(sender, args);
        }

        private void BackgroundMediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            BackgroundPlayer.MediaEnded(sender, args);
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            bool pauseAllowed = false, playling = sender.CurrentState == MediaPlayerState.Playing;
            double curMillis = sender.Position.TotalMilliseconds;
            double natMillis = sender.NaturalDuration.TotalMilliseconds;

            if (curMillis >= natMillis) pauseAllowed = false;

            FolderMusicDebug.DebugEvent.SaveText("StateChanged", "Playerstate: " + sender.CurrentState,
                "SMTC-State: " + smtc.PlaybackStatus, "PlayerPosition [s]: " + sender.Position.TotalMilliseconds,
                "PlayerDuration [s]: " + sender.NaturalDuration.TotalMilliseconds, "PauseAllowed: " + pauseAllowed,
                "LibraryIsPlaying: " + Library.Current.IsPlaying, Library.Current.CurrentPlaylist.CurrentSong);

            if (playling)
            {
                if (!Library.Current.IsPlaying) { }
                //Library.Current.IsPlaying = true;

                ringer.SetTimesIfIsDisposed();
            }
            else if (pauseAllowed) Library.Current.IsPlaying = false;

            CurrentPlaySong.Current.SaveAsync();
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            long curTicks = DateTime.Now.Ticks;
            bool timePassed = curTicks - smtcLastPressedTicks > smtcLastPressedMinDeltaTicks;

            FolderMusicDebug.DebugEvent.SaveText("MTCPressed", "SmtcLastPressedTicks: " + smtcLastPressedTicks,
                "CurrentButton: " + args.Button, "LastPressedDeltaTicks: " + (curTicks - smtcLastPressedTicks),
                "TimePassed: " + timePassed, Library.Current.CurrentPlaylist.CurrentSong);

            if (timePassed && smtcLastPressedTicks != 0)
            {
                if (!Library.IsLoaded())
                {
                    CurrentPlaySong.Current.Load();
                    FolderMusicDebug.DebugEvent.SaveText("SmtcLoad", Library.Current.CurrentPlaylist.CurrentSong);
                }

                MediaTransportControlButtonPressed(args.Button);
            }

            smtcLastPressedTicks = DateTime.Now.Ticks;
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControlsButton button)
        {
            switch (button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Library.Current.IsPlaying = true;
                    return;

                case SystemMediaTransportControlsButton.Pause:
                    Library.Current.IsPlaying = false;
                    return;

                case SystemMediaTransportControlsButton.Previous:
                    BackgroundPlayer.Previous();
                    return;

                case SystemMediaTransportControlsButton.Next:
                    BackgroundPlayer.Next(false);
                    return;
            }
        }

        private void OnSettingsPropertyChanged()
        {
            ringer.ReloadTimes();
        }

        private void OnSongsPropertyChanged(Playlist sender, SongsChangedEventArgs args)
        {
            if (Library.Current.CurrentPlaylist == sender)
            {
                FolderMusicDebug.DebugEvent.SaveText("SetOnSongs", Library.Current.CurrentPlaylist.CurrentSong);
                BackgroundPlayer.SetCurrent();
            }
        }

        private void OnShufflePropertyChanged(Playlist sender, ShuffleChangedEventArgs args)
        {
            if (Library.Current.CurrentPlaylist == sender)
            {
                FolderMusicDebug.DebugEvent.SaveText("SetOnShuffle", Library.Current.CurrentPlaylist.CurrentSong);
                BackgroundPlayer.SetCurrent();
            }
        }

        private void OnPlaylistsPropertyChanged(ILibrary sender, PlaylistsChangedEventArgs args)
        {
            FolderMusicDebug.DebugEvent.SaveText("SetOnPlaylists", Library.Current.CurrentPlaylist.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnPlayStateChanged(ILibrary sender, PlayStateChangedEventArgs args)
        {
            if (args.NewValue) BackgroundPlayer.Play();
            else BackgroundPlayer.Pause();
        }

        private void OnLoopPropertyChanged(Playlist sender, LoopChangedEventArgs args)
        {
            if (Library.Current.CurrentPlaylist == sender) SetLoopToBackgroundPlayer();
        }

        private void OnLibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            SetLoopToBackgroundPlayer();

            FolderMusicDebug.DebugEvent.SaveText("SetOnLibrary", Library.Current.CurrentPlaylist.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnCurrentSongPropertyChanged(Playlist sender, CurrentSongChangedEventArgs args)
        {
            FolderMusicDebug.DebugEvent.SaveText("SetOnCurrentSong", Library.Current.CurrentPlaylist.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnCurrentPlaylistPropertyChanged(ILibrary sender, CurrentPlaylistChangedEventArgs args)
        {
            FolderMusicDebug.DebugEvent.SaveText("SetOnCurrentPlaylist", Library.Current.CurrentPlaylist.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void TaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Dispose();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            Dispose();
        }

        private void Dispose()
        {
            ringer?.Dispose();
            //musicPlayer?.Dispose();
            //Unsubscribe();

            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();
        }

        private void Unsubscribe()
        {
            //smtc.ButtonPressed -= MediaTransportControlButtonPressed;

            BackgroundMediaPlayer.Current.CurrentStateChanged -= BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded -= BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened -= BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed -= BackgroundMediaPlayer_MediaFailed;

            Feedback.Current.OnCurrentPlaylistPropertyChanged -= OnCurrentPlaylistPropertyChanged;
            Feedback.Current.OnCurrentSongPropertyChanged -= OnCurrentSongPropertyChanged;
            Feedback.Current.OnLibraryChanged -= OnLibraryChanged;
            Feedback.Current.OnLoopPropertyChanged -= OnLoopPropertyChanged;
            Feedback.Current.OnPlayStateChanged -= OnPlayStateChanged;
            Feedback.Current.OnPlaylistsPropertyChanged -= OnPlaylistsPropertyChanged;
            Feedback.Current.OnShufflePropertyChanged -= OnShufflePropertyChanged;
            Feedback.Current.OnSettingsPropertyChanged -= OnSettingsPropertyChanged;
            Feedback.Current.OnSongsPropertyChanged -= OnSongsPropertyChanged;
        }
    }
}
