using System.Threading.Tasks;
using Windows.Media.Playback;

namespace BackgroundTask
{
    interface IBackgroundPlayer
    {
        Task Play();

        void Pause();

        void Next(bool fromEnded);

        void Previous();

        Task SetCurrent();

        void MediaOpened(MediaPlayer sender, object args);

        Task MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args);

        Task MediaEnded(MediaPlayer sender, object args);
    }
}
