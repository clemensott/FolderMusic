using MusicPlayer.Data;
using MusicPlayer.Data.Loop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using MusicPlayer.Data.Shuffle;

namespace BackgroundTask
{
    enum BackgroundPlayerType { Music, Ringer }

    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        private const long smtcLastPressedMinDeltaTicks = TimeSpan.TicksPerMillisecond * 300;
        private static long smtcLastPressedTicks = 0;

        private static BackgroundAudioTask task;

        private string taskId;
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
            string previousId = MobileDebug.Manager.Id;
            taskId = taskInstance.InstanceId.ToString();
            MobileDebug.Manager.SetIsBackground(taskId);
            MobileDebug.Manager.WriteEvent("Run", "PreviousId: " + previousId, "task==null: " + (task == null),
                "Hash: " + GetHashCode(), "PlayerHash: " + BackgroundMediaPlayer.Current.GetHashCode());

            deferral = taskInstance.GetDeferral();

            smtcLastPressedTicks = 1;
            smtc = SystemMediaTransportControls.GetForCurrentView();

            library = Library.Load(false);
            musicPlayer = new MusicPlayer(this, smtc, library);
            ringer = new Ringer(this, library);

            SetLoopToBackgroundPlayer();

            Unsubscribe(task);
            Subscribe(task = this);

            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += TaskCompleted;
        }

        private static void Subscribe(BackgroundAudioTask task)
        {
            var smtcType = task?.smtc.DisplayUpdater.Type.ToString() ?? "null";
            var smtcHash = task?.smtc.DisplayUpdater.GetHashCode().ToString() ?? "null";
            MobileDebug.Manager.WriteEvent("BackSubscribe", "SmtcType: " + smtcType, "SmtcHash: " + smtcHash);

            if (task == null) return;

            task.smtc.ButtonPressed += task.MediaTransportControlButtonPressed;

            BackgroundMediaPlayer.Current.CurrentStateChanged += task.BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded += task.BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened += task.BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed += task.BackgroundMediaPlayer_MediaFailed;

            task.library.LibraryChanged += task.OnLibraryChanged;
            task.library.PlayStateChanged += task.OnPlayStateChanged;
            task.library.PlaylistsChanged += task.OnPlaylistsChanged;
            task.library.CurrentPlaylistChanged += task.OnCurrentPlaylistChanged;

            task.Subscribe(task.library.CurrentPlaylist);
        }

        private static void Unsubscribe(BackgroundAudioTask task)
        {
            var smtcType = task?.smtc.DisplayUpdater.Type.ToString() ?? "null";
            var smtcHash = task?.smtc.DisplayUpdater.GetHashCode().ToString() ?? "null";
            MobileDebug.Manager.WriteEvent("BackUnsubscribe", "SmtcType: " + smtcType, "SmtcHash: " + smtcHash);

            if (task == null) return;

            task.smtc.ButtonPressed -= task.MediaTransportControlButtonPressed;

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
            MobileDebug.Manager.WriteEvent("MTCPressed", "Button: " + args.Button, library.CurrentPlaylist?.CurrentSong);

            if (library == null)
            {
                library = Library.Load(false);
                MobileDebug.Manager.WriteEvent("SmtcLoad", task.library.CurrentPlaylist?.CurrentSong);
            }

            MediaTransportControlButtonPressed(args.Button);
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

            MobileDebug.Manager.WriteEvent("StateChanged", "Playerstate: " + sender.CurrentState,
                "SMTC-State: " + smtc.PlaybackStatus, "PlayerPosition [s]: " + sender.Position.TotalMilliseconds,
                "PlayerDuration [s]: " + sender.NaturalDuration.TotalMilliseconds, "PauseAllowed: " + pauseAllowed,
                "LibraryIsPlaying: " + library.IsPlaying, library.CurrentPlaylist?.CurrentSong);

            if (playling) ringer.SetTimesIfIsDisposed();
            else if (pauseAllowed) library.IsPlaying = false;
        }

        private void MediaTransportControlButtonPressed2(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            long curTicks = DateTime.Now.Ticks;
            bool timePassed = curTicks - smtcLastPressedTicks > smtcLastPressedMinDeltaTicks;

            MobileDebug.Manager.WriteEvent("MTCPressed", taskId, "SmtcLastPressedTicks: " + smtcLastPressedTicks,
                "Button: " + args.Button, "LastPressedDeltaTicks: " + (curTicks - smtcLastPressedTicks),
                "TimePassed: " + timePassed, library.CurrentPlaylist?.CurrentSong);

            if (timePassed && smtcLastPressedTicks != 0)
            {
                if (library == null)
                {
                    library = Library.Load(false);
                    MobileDebug.Manager.WriteEvent("SmtcLoad", library.CurrentPlaylist?.CurrentSong);
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

        private void OnSettingsChanged()
        {
            ringer.ReloadTimes();
        }

        private void OnSongsChanged(ISongCollection sender, SongCollectionChangedEventArgs args)
        {
            MobileDebug.Manager.WriteEvent("SetOnSongs", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnShuffleSongsChanged(IShuffleCollection sender)
        {
            MobileDebug.Manager.WriteEvent("SetOnShuffleSongs", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnShuffleChanged(IPlaylist sender, ShuffleChangedEventArgs args)
        {
            MobileDebug.Manager.WriteEvent("SetOnShuffle", library.CurrentPlaylist?.CurrentSong,
                args.NewCurrentSong == args.OldCurrentSong);

            if (args.NewCurrentSong == args.OldCurrentSong) return;

            BackgroundPlayer.SetCurrent();
        }

        private void OnPlaylistsChanged(ILibrary sender, PlaylistsChangedEventArgs args)
        {
            MobileDebug.Manager.WriteEvent("BackgroundOnPlaylistChanged");

            if (args.NewCurrentPlaylist == args.OldCurrentPlaylist) return;

            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            MobileDebug.Manager.WriteEvent("SetOnPlaylists", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnPlayStateChanged(ILibrary sender, PlayStateChangedEventArgs args)
        {
            MobileDebug.Manager.WriteEvent("BackgroundPlayStateChanged", args.NewValue);

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

            MobileDebug.Manager.WriteEvent("SetOnLibrary", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnCurrentSongChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            MobileDebug.Manager.WriteEvent("SetOnCurrentSong", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnCurrentPlaylistChanged(ILibrary sender, CurrentPlaylistChangedEventArgs args)
        {
            MobileDebug.Manager.WriteEvent("SetOnCurrentPlaylist", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void Subscribe(IPlaylist playlist)
        {
            MobileDebug.Manager.WriteEvent("BackgroundSubscribePlaylist", playlist?.Name ?? "Name");
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.LoopChanged += OnLoopChanged;
            playlist.ShuffleChanged += OnShuffleChanged;

            playlist.Songs.CollectionChanged += OnSongsChanged;
            playlist.ShuffleSongs.Changed += OnShuffleSongsChanged;
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            MobileDebug.Manager.WriteEvent("BackgroundUnsubscribePlaylist", playlist?.Name ?? "Name");
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.ShuffleChanged -= OnShuffleChanged;

            playlist.Songs.CollectionChanged -= OnSongsChanged;
            playlist.ShuffleSongs.Changed -= OnShuffleSongsChanged;
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
        }
    }
}
