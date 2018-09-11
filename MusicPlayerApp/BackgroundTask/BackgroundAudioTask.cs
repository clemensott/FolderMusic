using MusicPlayer.Data;
using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;

namespace BackgroundTask
{
    enum BackgroundPlayerType { Music, Ringer }

    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        private static BackgroundAudioTask task;

        private ILibrary library;
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

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            string taskId = taskInstance.InstanceId.ToString();
            MobileDebug.Service.SetIsBackground(taskId);
            MobileDebug.Service.WriteEventPair("Run", "task == null: ", task == null,
                "this.Hash: ", GetHashCode(), "PlayerHash: ", BackgroundMediaPlayer.Current.GetHashCode());

            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += TaskCompleted;

            Unsubscribe(task);

            library = Library.LoadSimple(false);
            smtc = SystemMediaTransportControls.GetForCurrentView();
            task = this;

            musicPlayer = new MusicPlayer(smtc, library);
            ringer = new Ringer(this, library);

            Subscribe(task);

            library.LoadComplete();

            BackgroundPlayer.SetCurrent();

            MobileDebug.Service.WriteEvent("RunFinish");
        }

        private static void Subscribe(BackgroundAudioTask task)
        {
            var smtcType = task?.smtc.DisplayUpdater.Type.ToString() ?? "null";
            var smtcHash = task?.smtc.DisplayUpdater.GetHashCode().ToString() ?? "null";
            MobileDebug.Service.WriteEventPair("BackSubscribe", "SmtcType: ", smtcType, "SmtcHash: ", smtcHash);

            if (task == null) return;

            if (task.smtc != null) task.smtc.ButtonPressed += task.MediaTransportControlButtonPressed;

            BackgroundMediaPlayer.Current.CurrentStateChanged += task.BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded += task.BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened += task.BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed += task.BackgroundMediaPlayer_MediaFailed;

            task.library.LibraryChanged += task.OnLibraryChanged;
            task.library.PlayStateChanged += task.OnPlayStateChanged;
            task.library.PlaylistsChanged += task.OnPlaylistsChanged;
            task.library.Playlists.Changed += task.OnPlaylistCollectionChanged;
            task.library.CurrentPlaylistChanged += task.OnCurrentPlaylistChanged;

            task.Subscribe(task.library.CurrentPlaylist);
        }

        private static void Unsubscribe(BackgroundAudioTask task)
        {
            var smtcType = task?.smtc.DisplayUpdater.Type.ToString() ?? "null";
            var smtcHash = task?.smtc.DisplayUpdater.GetHashCode().ToString() ?? "null";
            MobileDebug.Service.WriteEventPair("BackUnsubscribe", "SmtcType: ", smtcType, "SmtcHash: ", smtcHash);

            if (task == null) return;

            if (task.smtc != null) task.smtc.ButtonPressed -= task.MediaTransportControlButtonPressed;

            BackgroundMediaPlayer.Current.CurrentStateChanged -= task.BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded -= task.BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened -= task.BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed -= task.BackgroundMediaPlayer_MediaFailed;

            task.library.LibraryChanged -= task.OnLibraryChanged;
            task.library.PlayStateChanged -= task.OnPlayStateChanged;
            task.library.PlaylistsChanged -= task.OnPlaylistsChanged;
            task.library.CurrentPlaylistChanged -= task.OnCurrentPlaylistChanged;

            task.Unsubscribe(task.library.CurrentPlaylist);
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            MobileDebug.Service.WriteEventPair("MTCPressed", "Button: ", args.Button,
                "Song: ", library.CurrentPlaylist?.CurrentSong);

            MediaTransportControlButtonPressed(args.Button);
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControlsButton button)
        {
            switch (button)
            {
                case SystemMediaTransportControlsButton.Play:
                    library.IsPlaying = true;
                    return;

                case SystemMediaTransportControlsButton.Pause:
                    library.IsPlaying = false;
                    return;

                case SystemMediaTransportControlsButton.Previous:
                    BackgroundPlayer.Previous();
                    return;

                case SystemMediaTransportControlsButton.Next:
                    BackgroundPlayer.Next(false);
                    return;
            }
        }

        public void SetLoopToBackgroundPlayer()
        {
            BackgroundMediaPlayer.Current.IsLoopingEnabled = library.CurrentPlaylist?.Loop == LoopType.Current;
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

            MobileDebug.Service.WriteEventPair("StateChanged", "Playerstate: ", sender.CurrentState,
                "SMTC-State: ", smtc.PlaybackStatus, "PlayerPosition [s]: ", sender.Position.TotalMilliseconds,
                "PlayerDuration [s]: ", sender.NaturalDuration.TotalMilliseconds, "PauseAllowed: ", pauseAllowed,
                "LibraryIsPlaying: ", library.IsPlaying, "CurrentSong: ", library.CurrentPlaylist?.CurrentSong,
                "LibIsCompleteLoaded: ", library.IsLoadedComplete);

            if (playling)
            {
                ringer.SetTimesIfIsDisposed();
            }
            else if (pauseAllowed) library.IsPlaying = false;
        }

        private void OnSettingsChanged()
        {
            ringer.ReloadTimes();
        }

        private void OnSongsChanged(ISongCollection sender, SongCollectionChangedEventArgs args)
        {
            //MobileDebug.Manager.WriteEvent("SetOnSongs", library.CurrentPlaylist?.CurrentSong);
            if (args.OldCurrentSong != args.NewCurrentSong) BackgroundPlayer.SetCurrent();
        }

        private void OnShuffleSongsChanged(IShuffleCollection sender)
        {
            //MobileDebug.Manager.WriteEvent("SetOnShuffleSongs", library.CurrentPlaylist?.CurrentSong);
            //BackgroundPlayer.SetCurrent();
        }

        private void OnShuffleChanged(IPlaylist sender, ShuffleChangedEventArgs args)
        {
            //MobileDebug.Manager.WriteEvent("SetOnShuffle", library.CurrentPlaylist?.CurrentSong,
            //args.NewCurrentSong == args.OldCurrentSong);

            if (args.NewCurrentSong == args.OldCurrentSong) return;

            BackgroundPlayer.SetCurrent();
        }

        private void OnPlaylistsChanged(ILibrary sender, PlaylistsChangedEventArgs args)
        {
            //MobileDebug.Manager.WriteEvent("BackgroundOnPlaylistChanged");

            args.OldPlaylists.Changed -= OnPlaylistCollectionChanged;
            args.NewPlaylists.Changed += OnPlaylistCollectionChanged;

            if (args.NewCurrentPlaylist == args.OldCurrentPlaylist) return;

            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            //MobileDebug.Manager.WriteEvent("SetOnPlaylists", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnPlaylistCollectionChanged(IPlaylistCollection sender, PlaylistCollectionChangedEventArgs args)
        {
            if (args.NewCurrentPlaylist == args.OldCurrentPlaylist) return;

            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            //MobileDebug.Manager.WriteEvent("SetOnPlaylists", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnPlayStateChanged(ILibrary sender, PlayStateChangedEventArgs args)
        {
            MobileDebug.Service.WriteEvent("BackgroundPlayStateChanged", args.NewValue);

            if (args.NewValue) BackgroundPlayer.Play();
            else BackgroundPlayer.Pause();
        }

        private void OnLoopChanged(IPlaylist sender, LoopChangedEventArgs args)
        {
            SetLoopToBackgroundPlayer();
        }

        private void OnLibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            SetLoopToBackgroundPlayer();

            //MobileDebug.Manager.WriteEvent("SetOnLibrary", library.CurrentPlaylist?.CurrentSong);
            if (args.OldCurrentPlaylist?.CurrentSong.Path != args.NewCurrentPlaylist?.CurrentSong?.Path)
            {
                BackgroundPlayer.SetCurrent();
            }
        }

        private void OnCurrentSongChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            //MobileDebug.Manager.WriteEvent("SetOnCurrentSong", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnCurrentPlaylistChanged(ILibrary sender, CurrentPlaylistChangedEventArgs args)
        {
            //MobileDebug.Manager.WriteEvent("SetOnCurrentPlaylist", library.CurrentPlaylist?.CurrentSong);
            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            BackgroundPlayer.SetCurrent();
        }

        private void Subscribe(IPlaylist playlist)
        {
            //MobileDebug.Manager.WriteEvent("BackgroundSubscribePlaylist", playlist?.Name ?? "Name");
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.LoopChanged += OnLoopChanged;
            playlist.ShuffleChanged += OnShuffleChanged;

            playlist.Songs.Changed += OnSongsChanged;
            playlist.ShuffleSongs.Changed += OnShuffleSongsChanged;
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            //MobileDebug.Manager.WriteEvent("BackgroundUnsubscribePlaylist", playlist?.Name ?? "Name");
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.ShuffleChanged -= OnShuffleChanged;

            playlist.Songs.Changed -= OnSongsChanged;
            playlist.ShuffleSongs.Changed -= OnShuffleSongsChanged;
        }

        private void TaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            library.IsPlaying = false;
            //MobileDebug.Service.WriteEvent("TaskCompleted");
            Cancel();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            library.IsPlaying = false;
            //MobileDebug.Service.WriteEvent("OnCanceled", reason);
            Cancel();
        }

        private void Cancel()
        {
            ringer?.Dispose();

            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();
        }
    }
}
