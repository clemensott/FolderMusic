using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;

namespace Background
{
    public sealed class Music
    {
        private BackgroundTaskDeferral deferral;
        private SystemMediaTransportControls systemMediaTransportControl;

        private string id;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            id = taskInstance.InstanceId.ToString();
            SaveText(DateTime.Now.Ticks, "Run", id);

            systemMediaTransportControl = SystemMediaTransportControls.GetForCurrentView();
            deferral = taskInstance.GetDeferral();

            Start(taskInstance);
        }

        private async void Start(IBackgroundTaskInstance taskInstance)
        {
            if (await IsTaskStarted())
            {
                BackgroundMediaPlayer.Shutdown();
                deferral.Complete();
                return;
            }

            await SetTaskStarted();

            SetSystemMediaTransportControlDefaultSettings();
            SetEvents(taskInstance);

            SetSong();
        }

        private async Task<bool> IsTaskStarted()
        {
            long ticks;

            try
            {
                string path = ApplicationData.Current.LocalFolder.Path + "\\" + "Started.txt";

                ticks = long.Parse(await PathIO.ReadTextAsync(path));
            }
            catch
            {
                ticks = 0;
            }

            return ticks + 10000000 > DateTime.Now.Ticks;
        }

        private async Task SetTaskStarted()
        {
            try
            {
                string filenameWithExtention = "Started.txt";
                string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;

                await PathIO.WriteTextAsync(path, DateTime.Now.Ticks.ToString());
            }
            catch { }
        }

        private void SetEvents(IBackgroundTaskInstance taskInstance)
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaOpened += BackgroundMediaPlayer_MediaOpened;

            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += Taskcompleted;
        }

        private void SetSystemMediaTransportControlDefaultSettings()
        {
            systemMediaTransportControl.IsEnabled = true;
            systemMediaTransportControl.IsPauseEnabled = true;
            systemMediaTransportControl.IsPlayEnabled = true;

            systemMediaTransportControl.IsPreviousEnabled = true;

            systemMediaTransportControl.ButtonPressed += MediaTransportControlButtonPressed;
        }

        private async void SetSong()
        {
            try
            {
                SaveText(DateTime.Now.Ticks, "Set", id);

                string path = @"C:\Data\Users\Public\Music\Music\Against the Current - Infinity.mp3";
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                BackgroundMediaPlayer.Current.SetFileSource(file);
            }
            catch
            {
                SaveText(DateTime.Now.Ticks, "Catch", id);
            }
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                BackgroundMediaPlayer.Current.Play();
                systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
            else if (sender.CurrentState == MediaPlayerState.Paused)
            {
                BackgroundMediaPlayer.Current.Pause();
                systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
        }

        private void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            SaveText(DateTime.Now.Ticks, "Open", id);

            sender.Play();
            UpdateSystemMediaTransportControl();

            SetStartedTimeInDataBank();
        }

        private async void SetStartedTimeInDataBank()
        {
            try
            {
                string filename1WithExtention = "StartedBackup1.txt", filename2WithExtention = "StartedBackup2.txt",
                    path = ApplicationData.Current.LocalFolder.Path + "\\" + "Started.txt", saveFilenameWithExtention,
                    saveTicks = await PathIO.ReadTextAsync(path), backup1Ticks = "", backup2Ticks = "";

                try
                {
                    backup1Ticks = await PathIO.ReadTextAsync(
                        ApplicationData.Current.LocalFolder.Path + "\\" + filename1WithExtention);
                }
                catch
                {
                    StorageFile file = await ApplicationData.Current.LocalFolder.
                        CreateFileAsync(filename1WithExtention, CreationCollisionOption.ReplaceExisting);

                    await FileIO.WriteTextAsync(file, saveTicks);
                }

                try
                {
                    backup2Ticks = await PathIO.ReadTextAsync(
                        ApplicationData.Current.LocalFolder.Path + "\\" + filename2WithExtention);
                }
                catch
                {
                    StorageFile file = await ApplicationData.Current.LocalFolder.
                        CreateFileAsync(filename2WithExtention, CreationCollisionOption.ReplaceExisting);

                    await FileIO.WriteTextAsync(file, saveTicks);
                }

                saveFilenameWithExtention = long.Parse(backup1Ticks) > long.Parse(backup2Ticks) ?
                    filename2WithExtention : filename1WithExtention;

                await PathIO.WriteTextAsync(ApplicationData.Current.LocalFolder.Path + "\\" + saveFilenameWithExtention, saveTicks);
            }
            catch { }
        }

        private void UpdateSystemMediaTransportControl()
        {
            SaveText(DateTime.Now.Ticks, "Update", id);
            systemMediaTransportControl.DisplayUpdater.Type = MediaPlaybackType.Music;

            systemMediaTransportControl.DisplayUpdater.MusicProperties.Title = "Infinity";
            systemMediaTransportControl.DisplayUpdater.MusicProperties.Artist = "Against the Current";

            systemMediaTransportControl.DisplayUpdater.Update();
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControls sender,
           SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (args.Button == SystemMediaTransportControlsButton.Play)
            {
                BackgroundMediaPlayer.Current.Play();
                sender.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
            else if (args.Button == SystemMediaTransportControlsButton.Pause)
            {
                BackgroundMediaPlayer.Current.Pause();
                sender.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
            else
            {
                sender.IsNextEnabled = !sender.IsNextEnabled;
            }
        }

        private void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            SaveText(DateTime.Now.Ticks, "Complete", id, sender.Name);

            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            SaveText(DateTime.Now.Ticks, "Cancel", id, reason);

            BackgroundMediaPlayer.Shutdown();
            deferral.Complete();
        }

        private async Task SaveText(params object[] objs)
        {
            try
            {
                string text = "";
                string filename = string.Format("Text{0}.txt", new Random().Next(1000));
                StorageFile file = await ApplicationData.Current.LocalFolder.
                    CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                foreach (object obj in objs) text += obj.ToString() + ";";

                text = text.TrimEnd(';');

                await PathIO.WriteTextAsync(file.Path, text);
            }
            catch { }
        }
    }
}
