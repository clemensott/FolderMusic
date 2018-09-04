using Windows.Media.Playback;

namespace BackgroundTask
{
    interface IBackgroundPlayer
    {
        void Play();

        void Pause();

        void Next(bool fromEnded);

        void Previous();

        void SetCurrent();

        void MediaOpened(MediaPlayer sender, object args);

        void MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args);

        void MediaEnded(MediaPlayer sender, object args);
    }
}
