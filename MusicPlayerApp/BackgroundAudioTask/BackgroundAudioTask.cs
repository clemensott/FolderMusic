using PlaylistSong;
using System;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;

namespace BackgroundAudioTask
{
    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
        private SystemMediaTransportControls _systemMediaTransportControl;

        private bool autoPlay;

        private bool IsPlaying { get { return BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing; } }

        private Song CurrentSong { get { return CurrentPlaylist.CurrentSong; } }

        private Playlist CurrentPlaylist { get { return Library.Current.CurrentPlaylist; } }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _systemMediaTransportControl = SystemMediaTransportControls.GetForCurrentView();

            SetSystemMediaTransportControlDefaultSettings();

            BackgroundMediaPlayer.MessageReceivedFromForeground += MessageReceivedFromForeground;
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded += BackgroundMediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaOpened += BackgroundMediaPlayer_MediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed += BackgroundMediaPlayer_MediaFailed;

            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += Taskcompleted;

            _deferral = taskInstance.GetDeferral();

            Library.Load();
            PlaySong(await Library.LoadPlayCommand());
        }

        private void SetSystemMediaTransportControlDefaultSettings()
        {
            _systemMediaTransportControl.IsEnabled = true;
            _systemMediaTransportControl.IsPauseEnabled = true;
            _systemMediaTransportControl.IsPlayEnabled = true;
            _systemMediaTransportControl.IsPreviousEnabled = true;
            _systemMediaTransportControl.IsNextEnabled = true;
            _systemMediaTransportControl.IsRewindEnabled = true;
            _systemMediaTransportControl.IsFastForwardEnabled = true;

            _systemMediaTransportControl.ButtonPressed += MediaTransportControlButtonPressed;
        }

        private void MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            int index;
            string currentSongPath;
            ValueSet valueSet = e.Data;

            foreach (string key in valueSet.Keys)
            {
                switch (key)
                {
                    case "PlaySong":
                        SetCurrentSongIndex(int.Parse(valueSet[key].ToString()));
                        CurrentPlaylist.SongPositionMilliseconds = 0;
                        PlaySong(true);
                        return;

                    case "Play":
                        Play();
                        return;

                    case "Pause":
                        Pause();
                        return;

                    case "Previous":
                        Previous();
                        return;

                    case "Next":
                        Next(IsPlaying);
                        return;

                    case "Loop":
                        index = int.Parse(valueSet[key].ToString());
                        Library.Current[index].SetNextLoop();
                        SetLoopToBackgroundPlayer();
                        return;

                    case "Shuffle":
                        index = int.Parse(valueSet["Index"].ToString());
                        Library.Current[index].SetShuffleList(valueSet);
                        return;

                    case "CurrentPlaylistIndex":
                        Library.Current.CurrentPlaylistIndex = int.Parse(valueSet[key].ToString());
                        PlaySong(bool.Parse(valueSet["Play"].ToString()));

                        Library.SaveAsync();
                        return;

                    case "GetCurrent":
                        SendGetCurrent();
                        return;

                    case "Load":
                        currentSongPath = CurrentSong.Path;

                        Library.Load();

                        if (currentSongPath != CurrentSong.Path) PlaySong(IsPlaying);
                        return;

                    case "PlaylistPageTap":
                        Library.Current.CurrentPlaylistIndex = int.Parse(valueSet[key].ToString());

                        if (valueSet["ShuffleOff"].ToString() == true.ToString())
                        {
                            CurrentPlaylist.Shuffle = ShuffleKind.Off;
                        }

                        SetCurrentSongIndex(int.Parse(valueSet["CurrentSongIndex"].ToString()));
                        PlaySong(true);

                        Library.SaveAsync();
                        return;

                    case "RemoveSong":
                        currentSongPath = CurrentSong.Path;

                        Library.Current.RemoveSongFromCurrentPlaylist(int.Parse(valueSet[key].ToString()));

                        if (currentSongPath != CurrentSong.Path) PlaySong(IsPlaying);
                        return;

                    case "RemovePlaylist":
                        currentSongPath = CurrentSong.Path;

                        Library.Current.DeleteAt(int.Parse(valueSet[key].ToString()));

                        if (currentSongPath != CurrentSong.Path) PlaySong(IsPlaying);
                        return;
                }
            }
        }

        private void SetCurrentSongIndex(int index)
        {
            if (CurrentPlaylist.Shuffle == ShuffleKind.Complete)
            {
                bool first = index < CurrentPlaylist.CurrentSongIndex;
                bool last = index > CurrentPlaylist.CurrentSongIndex;

                int count = Math.Abs(index - CurrentPlaylist.CurrentSongIndex);

                if (last)
                {
                    CurrentPlaylist.SetNextSong(count);
                }
                else if (first)
                {
                    CurrentPlaylist.SetPreviousSong(count);
                }

                SendNewSong(first, count);
            }

            CurrentPlaylist.CurrentSongIndex = index;
        }

        private void SetLoopToBackgroundPlayer()
        {
            BackgroundMediaPlayer.Current.IsLoopingEnabled = CurrentPlaylist.Loop == LoopKind.Current;
        }

        private void Play()
        {
            autoPlay = true;

            if (BackgroundMediaPlayer.Current.NaturalDuration.TotalMilliseconds == 0)
            {
                PlaySong(true);
                return;
            }

            BackgroundMediaPlayer.Current.Play();
        }

        private void Pause()
        {
            autoPlay = false;
            BackgroundMediaPlayer.Current.Pause();
        }

        private void Previous()
        {
            CurrentPlaylist.SetPreviousSong();
            PlaySong(IsPlaying);

            if (CurrentPlaylist.Shuffle == ShuffleKind.Complete)
            {
                SendNewSong(true);
            }
        }

        private void Next(bool autoPlay, bool fromEnded = false)
        {
            bool stop = CurrentPlaylist.SetNextSong();
            autoPlay = fromEnded ? autoPlay && !stop : autoPlay;

            PlaySong(autoPlay);

            if (CurrentPlaylist.Shuffle == ShuffleKind.Complete)
            {
                SendNewSong(false);
            }
        }

        private async void PlaySong(bool autoPlay)
        {
            StorageFile file;
            this.autoPlay = autoPlay;
            BackgroundMediaPlayer.Current.AutoPlay = false;

            try
            {
                file = await CurrentSong.GetStorageFile();

                if (CurrentSong.Path == "")
                {
                    return;
                }
                else if (file == null)
                {
                    SendSkip();
                    return;
                }

                BackgroundMediaPlayer.Current.SetFileSource(file);
            }
            catch { }
        }

        private void UpdateSystemMediaTransportControl()
        {
            _systemMediaTransportControl.DisplayUpdater.Type = MediaPlaybackType.Music;
            _systemMediaTransportControl.DisplayUpdater.MusicProperties.Title = CurrentSong.Title;
            _systemMediaTransportControl.DisplayUpdater.MusicProperties.Artist = CurrentSong.Artist;
            _systemMediaTransportControl.DisplayUpdater.Update();
        }

        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                _systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
        }

        private void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            if (CurrentPlaylist.SongPositionMilliseconds != 0)
            {
                sender.Position = TimeSpan.FromMilliseconds(CurrentPlaylist.SongPositionMilliseconds);
            }

            if (CurrentSong.NaturalDurationMilliseconds == 1)
            {
                CurrentSong.NaturalDurationMilliseconds = sender.NaturalDuration.TotalMilliseconds;
            }

            if (autoPlay) sender.Play();

            UpdateSystemMediaTransportControl();

            SendCurrentSong();
            SaveSongIndexAndMilliseconds();
        }

        private void BackgroundMediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            if (args.Error == MediaPlayerError.Unknown)
            {
                PlaySong(autoPlay);
                return;
            }

            SendSkip();
        }

        private void BackgroundMediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            Next(true, true);
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Play();
                    break;

                case SystemMediaTransportControlsButton.Pause:
                    Pause();
                    SendPause();
                    break;

                case SystemMediaTransportControlsButton.Previous:
                    Previous();
                    break;

                case SystemMediaTransportControlsButton.Next:
                    Next(IsPlaying);
                    break;
            }
        }

        private void SendPause()
        {
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { "Pause", "" } });
        }

        private void SendCurrentSong()
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add("Current", CurrentPlaylist.CurrentSongIndex.ToString());
            valueSet.Add("Position", CurrentPlaylist.SongPositionMilliseconds.ToString());
            valueSet.Add("Duration", CurrentSong.NaturalDurationMilliseconds.ToString());

            BackgroundMediaPlayer.SendMessageToForeground(valueSet);
        }

        private void SendCurrentPlaylistShuffle()
        {
            BackgroundMediaPlayer.SendMessageToForeground(
                CurrentPlaylist.GetShuffleAsValueSet(Library.Current.CurrentPlaylistIndex));
        }

        private void SendNewSong(bool first, int count = 1)
        {
            string paths = "";

            for (int i = 0; i < count; i++)
            {
                paths += GetPathFromShuffleList(first, count, i) + ";";
            }

            ValueSet valueSet = new ValueSet { { "New", first.ToString() } };
            valueSet.Add("Path", paths);

            BackgroundMediaPlayer.SendMessageToForeground(valueSet);

            Library.SaveAsync();
        }

        private void SendGetCurrent()
        {
            if (CurrentPlaylist.Shuffle == ShuffleKind.Complete)
            {
                SendCurrentPlaylistShuffle();
                return;
            }

            SendCurrentSong();
        }

        private void SendSkip()
        {
            ValueSet valueSet = new ValueSet { { "Skip", CurrentPlaylist.CurrentSongIndex.ToString() } };
            BackgroundMediaPlayer.SendMessageToForeground(valueSet);

            Next(true);
        }

        private string GetPathFromShuffleList(bool first, int count, int relativ)
        {
            int index = first ? count - 1 - relativ : CurrentPlaylist.ShuffleList.Count - count + relativ;
            return CurrentPlaylist.GetShuffleSong(index).Path;
        }

        private void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            BackgroundMediaPlayer.Shutdown();
            _deferral.Complete();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            SaveSongIndexAndMilliseconds();
            Library.SaveAsync();

            BackgroundMediaPlayer.Shutdown();
            _deferral.Complete();
        }

        private void SaveSongIndexAndMilliseconds()
        {
            Library.SaveSongIndexAndMilliseconds(CurrentPlaylist.CurrentSongIndex,
                BackgroundMediaPlayer.Current.Position.TotalMilliseconds);
        }
    }
}
