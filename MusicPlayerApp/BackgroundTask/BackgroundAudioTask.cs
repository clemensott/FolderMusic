using System;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Media.Playback;
using MusicPlayer;
using MusicPlayer.Communication;
using MusicPlayer.Handler;
using MusicPlayer.Models;
using MusicPlayer.Models.Background;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Enums;
using System.Threading.Tasks;

namespace BackgroundTask
{
    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        private const string dataFileName = "backgroundData.xml";

        private static BackgroundAudioTask lastTask;

        private BackgroundTaskDeferral deferral;
        private BackgroundPlayerHandler musicPlayer;
        private Timer timer;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            string taskId = taskInstance.InstanceId.ToString();
            MobileDebug.Service.SetIsBackground(taskId);
            MobileDebug.Service.WriteEventPair("Run1",
                "task == null", lastTask == null, "this.Hash", GetHashCode());

            try
            {
                deferral = taskInstance.GetDeferral();
                taskInstance.Canceled += OnCanceled;
                taskInstance.Task.Completed += TaskCompleted;

                lastTask?.musicPlayer?.Dispose();
                lastTask = this;

                BackgroundPlaylist playlist;

                try
                {
                    playlist = await IO.LoadObjectAsync<BackgroundPlaylist>(dataFileName);
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("Load background playlist error1", e);
                    playlist = new BackgroundPlaylist();
                }

                playlist.LoopChanged += Playlist_LoopChanged;
                playlist.SongsChanged += Playlist_SongsChanged;

                Song? song = CurrentPlaySong.Current.Song;
                TimeSpan position = TimeSpan.FromTicks(CurrentPlaySong.Current.PositionTicks);
                musicPlayer = await BackgroundPlayerHandler.Start(playlist, song, position);
                musicPlayer.CurrentSongChanged += MusicPlayer_CurrentSongChanged;

                BackgroundCommunicator.Start(musicPlayer);

                timer = new Timer(Timer_Tick, null, Timeout.Infinite, Timeout.Infinite);
                BackgroundMediaPlayer.Current.CurrentStateChanged += OnStateChanged;
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("Run error", e);
            }
            MobileDebug.Service.WriteEventPair("RunFinish",
                "This", GetHashCode(), "mp", musicPlayer.GetHashCode());
        }

        private void OnStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing) timer.Change(0, 2000);
            else timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private static void Timer_Tick(object state)
        {
            CurrentPlaySong.Current.PositionTicks = BackgroundMediaPlayer.Current.Position.Ticks;
        }

        private async void Playlist_LoopChanged(object sender, ChangedEventArgs<LoopType> e)
        {
            await SavePlaylist();
        }

        private async void Playlist_SongsChanged(object sender, ChangedEventArgs<Song[]> e)
        {
            await SavePlaylist();
        }

        private async Task SavePlaylist()
        {
            try
            {
                await IO.SaveObjectAsync(dataFileName, musicPlayer.Playlist);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("Save playlist error", e);
            }
        }

        private void MusicPlayer_CurrentSongChanged(object sender, ChangedEventArgs<Song?> e)
        {
            CurrentPlaySong.Current.Song = e.NewValue;
        }

        private void TaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            //MobileDebug.Service.WriteEvent("TaskCompleted");
            Cancel();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //MobileDebug.Service.WriteEvent("OnCanceled", reason);
            Cancel();
        }

        private void Cancel()
        {
            musicPlayer?.Stop();
            timer.Dispose();
            BackgroundCommunicator.Stop();

            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();
        }
    }
}
