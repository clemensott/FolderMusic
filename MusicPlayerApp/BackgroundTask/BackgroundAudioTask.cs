using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using MusicPlayer.Data.SubscriptionsHandler;
using System;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;

namespace BackgroundTask
{
    enum BackgroundPlayerType { Music, Ringer }

    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        private const string completeFileName = "Data.xml", backupFileName = "Data.bak",
              simpleFileName = "SimpleData.xml", currentSongFileName = "CurrentSong.xml";

        private static BackgroundAudioTask task;

        private AutoSaveLoad saveLoad;
        private LibrarySubscriptionsHandler lsh;
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

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            string taskId = taskInstance.InstanceId.ToString();
            MobileDebug.Service.SetIsBackground(taskId);
            MobileDebug.Service.WriteEventPair("Run", "task == null: ", task == null,
                "this.Hash: ", GetHashCode(), "PlayerHash: ", BackgroundMediaPlayer.Current.GetHashCode());

            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += TaskCompleted;

            Unsubscribe(task);

            saveLoad = new AutoSaveLoad(completeFileName, backupFileName, simpleFileName, currentSongFileName);
            library = await saveLoad.LoadSimple(false);
            lsh = LibrarySubscriptionsHandler.GetInstance(library);
            smtc = SystemMediaTransportControls.GetForCurrentView();
            task = this;

            musicPlayer = new MusicPlayer(smtc, library);
            ringer = new Ringer(this, library);

            await saveLoad.LoadComplete(library);
            saveLoad.Add(library);

            Subscribe(task);

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

            task.lsh.Subscribe(task.library);

            task.lsh.PlayStateChanged += task.OnPlayStateChanged;
            task.lsh.CurrentPlaylistChanged += task.OnCurrentPlaylistChanged;
            task.lsh.SettingsChanged += task.OnSettingsChanged;
            task.lsh.CurrentPlaylist.CurrentSongChanged += task.OnCurrentSongChanged;
            task.lsh.CurrentPlaylist.LoopChanged += task.OnLoopChanged;
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

            task.lsh.PlayStateChanged -= task.OnPlayStateChanged;
            task.lsh.CurrentPlaylistChanged -= task.OnCurrentPlaylistChanged;
            task.lsh.SettingsChanged -= task.OnSettingsChanged;
            task.lsh.CurrentPlaylist.CurrentSongChanged -= task.OnCurrentSongChanged;
            task.lsh.CurrentPlaylist.LoopChanged -= task.OnLoopChanged;

            task.lsh.Unsubscribe(task.library);
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
                "LibIsCompleteLoaded: ", library.IsLoaded);

            if (playling)
            {
                ringer.SetTimesIfIsDisposed();
            }
            else if (pauseAllowed) library.IsPlaying = false;
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            ringer.ReloadTimes();
        }

        private void OnPlayStateChanged(object sender, SubscriptionsEventArgs<ILibrary, PlayStateChangedEventArgs> e)
        {
            MobileDebug.Service.WriteEvent("BackgroundPlayStateChanged", e.Base.NewValue);

            if (e.Base.NewValue) BackgroundPlayer.Play();
            else BackgroundPlayer.Pause();
        }

        private void OnLoopChanged(object sender, EventArgs args)
        {
            SetLoopToBackgroundPlayer();
        }

        private void OnCurrentSongChanged(object sender, EventArgs args)
        {
            //MobileDebug.Manager.WriteEvent("SetOnCurrentSong", library.CurrentPlaylist?.CurrentSong);
            BackgroundPlayer.SetCurrent();
        }

        private void OnCurrentPlaylistChanged(object sender,SubscriptionsEventArgs<ILibrary, CurrentPlaylistChangedEventArgs>e)
        {
            BackgroundPlayer.SetCurrent();
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
