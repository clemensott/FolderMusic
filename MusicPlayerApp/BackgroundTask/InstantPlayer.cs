using MusicPlayer.Data;
using System;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;

namespace BackgroundTask
{
    delegate void FinishedEventHandler(InstantPlayer sender, bool sucessfull);

    class InstantPlayer
    {
        public event FinishedEventHandler Finished;

        public bool HasFinished { get; private set; }

        public bool Sucessfull { get; private set; }

        public ILibrary Library { get; private set; }

        public InstantPlayer(ILibrary library)
        {
            Library = library;
        }

        public void Set()
        {
            Song song = Library?.CurrentPlaylist?.CurrentSong;

            try
            {
                StorageFile file = song?.GetStorageFile();
                
                try
                {
                    Subscribe();

                    BackgroundMediaPlayer.Current.AutoPlay = true;
                    BackgroundMediaPlayer.Current.Volume = 0;
                    BackgroundMediaPlayer.Current.SetFileSource(file);

                    Task.Delay(5000).Wait(6000);

                    if (HasFinished) return;

                    MobileDebug.Manager.WriteEvent("InstantPlayerSetNotHappend", song);

                    Unsubscribe();
                    Sucessfull = false;
                    HasFinished = true;

                    Finished?.Invoke(this, false);
                }
                catch (Exception e)
                {
                    Unsubscribe();
                    MobileDebug.Manager.WriteEvent("InstantSetFileFail", e, song);
                }
            }
            catch (Exception e)
            {
                MobileDebug.Manager.WriteEvent("InstantSetGetFileFail", e, song);
                Finished?.Invoke(this, false);
            }
        }

        private void Subscribe()
        {
            if (Library != null) Library.PlayStateChanged += OnPlayStateChanged;

            BackgroundMediaPlayer.Current.MediaOpened += OnMediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed += OnMediaFailed;
            BackgroundMediaPlayer.Current.CurrentStateChanged += OnCurrentStateChanged;
        }

        private void Unsubscribe()
        {
            if (Library != null) Library.PlayStateChanged -= OnPlayStateChanged;

            BackgroundMediaPlayer.Current.MediaOpened -= OnMediaOpened;
            BackgroundMediaPlayer.Current.MediaFailed -= OnMediaFailed;
            BackgroundMediaPlayer.Current.CurrentStateChanged -= OnCurrentStateChanged;
        }

        private void OnPlayStateChanged(ILibrary sender, PlayStateChangedEventArgs args)
        {
            MobileDebug.Manager.WriteEvent("InstantPlayerPlaystateChanged", args.NewValue);

            if (HasFinished) return;

            if (args.NewValue) BackgroundMediaPlayer.Current.Play();
            else BackgroundMediaPlayer.Current.Pause();
        }

        private void OnMediaOpened(MediaPlayer sender, object args)
        {
            MobileDebug.Manager.WriteEvent("InstantOpen", Library.IsPlaying, sender.CurrentState);

            double position = Library.CurrentPlaylist.CurrentSongPositionPercent;

            sender.Position = TimeSpan.FromDays(sender.NaturalDuration.TotalDays * position);
        }

        private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Unsubscribe();

            if (HasFinished) return;

            Sucessfull = false;
            HasFinished = true;

            Finished?.Invoke(this, false);
        }

        private void OnCurrentStateChanged(MediaPlayer sender, object args)
        {
            MobileDebug.Manager.WriteEvent("InstantPlayerCurrentState", sender.CurrentState, Library.IsPlaying);
            if (sender.CurrentState != MediaPlayerState.Playing) return;

            if (!Library.IsPlaying) sender.Pause();

            sender.Volume = 1;

            Unsubscribe();

            if (HasFinished) return;

            Sucessfull = true;
            HasFinished = true;

            Finished?.Invoke(this, true);
        }
    }
}
