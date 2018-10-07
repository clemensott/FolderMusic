using MusicPlayer.Data;
using System;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace FolderMusic
{
    public sealed partial class Slider : UserControl
    {
        private const double intervall = 1000, zoomHalfWidth = 0.05;

        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register("IsIndeterminate", typeof(bool), typeof(Slider),
                new PropertyMetadata(false, new PropertyChangedCallback(OnIsIndeterminatePropertyChanged)));

        private static void OnIsIndeterminatePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var value = (bool)e.NewValue;
        }

        public static readonly DependencyProperty LibraryProperty =
            DependencyProperty.Register("Library", typeof(ILibrary), typeof(Slider),
                new PropertyMetadata(null, new PropertyChangedCallback(OnLibraryPropertyChanged)));

        private static void OnLibraryPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var oldValue = (ILibrary)e.OldValue;
            var newValue = (ILibrary)e.NewValue;

            s.Unsubscribe(oldValue);
            s.Subscribe(newValue);
        }

        public static readonly DependencyProperty PlayerProperty =
            DependencyProperty.Register("Player", typeof(MediaPlayer), typeof(Slider),
                new PropertyMetadata(null, new PropertyChangedCallback(OnPlayerPropertyChanged)));

        private static void OnPlayerPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var oldValue = (MediaPlayer)e.OldValue;
            var newValue = (MediaPlayer)e.NewValue;

            if (oldValue != null) oldValue.CurrentStateChanged -= s.MediaPlayer_CurrentStateChanged;
            if (newValue != null) newValue.CurrentStateChanged += s.MediaPlayer_CurrentStateChanged;

            s.SetValuesSafe();

            s.IsIndeterminate = newValue.CurrentState != MediaPlayerState.Playing &&
                newValue.CurrentState != MediaPlayerState.Paused;
        }

        private bool playerPositionEnabled = true;
        private DateTime previousUpdatedTime;
        private DispatcherTimer timer;

        public bool IsIndeterminate
        {
            get { return (bool)GetValue(IsIndeterminateProperty); }
            set { SetValue(IsIndeterminateProperty, value); }
        }

        public double PlayerPositionMilliseconds
        {
            get
            {
                return Player?.Position.TotalMilliseconds ?? MusicPlayer.Data.Library.DefaultSongsPositionMillis;
            }
        }

        public double PlayerNaturalDurationMilliseconds
        {
            get
            {
                return Player?.NaturalDuration.TotalMilliseconds ?? Song.DefaultDuration;
            }
        }

        public MediaPlayer Player
        {
            get { return (MediaPlayer)GetValue(PlayerProperty); }
            set { SetValue(PlayerProperty, value); }
        }

        public ILibrary Library
        {
            get { return (ILibrary)GetValue(LibraryProperty); }
            set { SetValue(LibraryProperty, value); }
        }

        public Slider()
        {
            this.InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(intervall);
            timer.Tick += Timer_Tick;

            Window.Current.Activated += Window_Activated;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement highestParent = this;

            while (highestParent.Parent as FrameworkElement != null) highestParent = highestParent.Parent as FrameworkElement;

            highestParent.PointerExited += HighestParent_PointerExited;
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            try
            {
                if (e.WindowActivationState == CoreWindowActivationState.PointerActivated) return;

                bool isActiv = e.WindowActivationState == CoreWindowActivationState.CodeActivated;

                if (!isActiv) StopTimer();
                else if (Player?.CurrentState == MediaPlayerState.Playing) StartTimer();
            }
            catch { }
        }

        private void Subscribe(ILibrary library)
        {
            if (library == null) return;

            library.PlayStateChanged += OnPlayStateChanged;
            library.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            library.PlaylistsChanged += OnPlaylistsChanged;
            library.Playlists.Changed += OnPlaylistCollectionChanged;
            library.LibraryChanged += OnLibraryChanged;

            Subscribe(library.CurrentPlaylist);
        }

        private void Unsubscribe(ILibrary library)
        {
            if (library == null) return;

            library.PlayStateChanged -= OnPlayStateChanged;
            library.CurrentPlaylistChanged -= OnCurrentPlaylistChanged;
            library.PlaylistsChanged -= OnPlaylistsChanged;
            library.Playlists.Changed -= OnPlaylistCollectionChanged;
            library.LibraryChanged -= OnLibraryChanged;

            Unsubscribe(library.CurrentPlaylist);
        }

        private void OnPlaylistsChanged(ILibrary sender, PlaylistsChangedEventArgs args)
        {
            args.OldPlaylists.Changed -= OnPlaylistCollectionChanged;
            Unsubscribe(args.OldCurrentPlaylist);
            args.NewPlaylists.Changed += OnPlaylistCollectionChanged;
            Subscribe(args.NewCurrentPlaylist);
        }

        private void OnPlaylistCollectionChanged(IPlaylistCollection sender, PlaylistCollectionChangedEventArgs args)
        {
            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);
        }

        private void OnLibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            SetValuesSafe();
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.CurrentSongPositionChanged += OnCurrentSongPositionChanged;

            Subscribe(playlist.CurrentSong);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.CurrentSongPositionChanged -= OnCurrentSongPositionChanged;

            Unsubscribe(playlist.CurrentSong);
        }

        private void Subscribe(Song song)
        {
            if (song == null) return;

            song.DurationChanged += Song_DurationChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (song == null) return;

            song.DurationChanged -= Song_DurationChanged;
        }

        private void OnPlayStateChanged(ILibrary sender, PlayStateChangedEventArgs args)
        {
            if (args.NewValue) StartTimer();
            else StopTimer();
        }

        private void OnCurrentPlaylistChanged(ILibrary sender, CurrentPlaylistChangedEventArgs args)
        {
            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            SetValuesSafe();
        }

        private void OnCurrentSongChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            Unsubscribe(args.OldCurrentSong);
            Subscribe(args.NewCurrentSong);

            SetValuesSafe();
            previousUpdatedTime = DateTime.Now;
        }

        private void OnCurrentSongPositionChanged(IPlaylist sender, CurrentSongPositionChangedEventArgs args)
        {
            SetValuesSafe();
            previousUpdatedTime = DateTime.Now;
        }

        private void Song_DurationChanged(Song sender, SongDurationChangedEventArgs args)
        {
            SetValuesSafe();
        }

        private void SetValuesSafe()
        {
            Utils.DoSafe(SetValues);
        }

        private void SetValues()
        {
            try
            {
                double percent = Library?.CurrentPlaylist?.CurrentSongPositionPercent ?? 0;
                double duration = Library?.CurrentPlaylist?.CurrentSong?.DurationMilliseconds ?? Song.DefaultDuration;

                if (duration == 0)
                {
                    duration = Song.DefaultDuration;
                    MobileDebug.Service.WriteEvent("Slider.SetValues2", "Lib != null", Library != null,
                        "Playlist != null", Library?.CurrentPlaylist != null, 
                        "Song != null", Library?.CurrentPlaylist?.CurrentSong != null,
                        "Duration != null", Library?.CurrentPlaylist?.CurrentSong?.DurationMilliseconds != null);
                }

                try
                {
                    sld.Value = percent;
                    tblPosition.Text = GetShowTime(percent * duration);
                    tblDuration.Text = GetShowTime(duration);
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEvent("Slider.SetValues3", e, percent, duration);
                }
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("Slider.SetValues1", e);
            }
        }

        private string GetShowTime(double totalMilliseconds)
        {
            try
            {
                int totalSeconds = Convert.ToInt32(totalMilliseconds / 1000);
                int seconds = totalSeconds % 60, minutes = (totalSeconds / 60) % 60, hours = totalSeconds / 3600;
                string time = string.Empty;

                time += hours > 0 ? hours.ToString() + ":" : string.Empty;
                time += hours > 0 ? string.Format("{0,2}", minutes) : minutes.ToString();
                time += string.Format(":{0,2}", seconds);

                return time.Replace(" ", "0");
            }
            catch { }

            return "Catch";
        }

        private void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            Utils.DoSafe(SetTimer);
            Utils.DoSafe(SetIsIndeterminate);
        }

        private void SetTimer()
        {
            if (Player.CurrentState == MediaPlayerState.Playing) StartTimer();
            else StopTimer();
        }

        private void SetIsIndeterminate()
        {
            MediaPlayerState state = Player.CurrentState;
            IsIndeterminate = state != MediaPlayerState.Playing && state != MediaPlayerState.Paused;
        }

        public void StartTimer()
        {
            previousUpdatedTime = DateTime.Now;
            Timer_Tick(timer, null);
        }

        public void StopTimer()
        {
            timer.Stop();
        }

        private void Timer_Tick(object sender, object e)
        {
            timer.Stop();
            timer.Interval = TimeSpan.FromMilliseconds(intervall - (PlayerPositionMilliseconds % intervall));
            timer.Start();

            if (!playerPositionEnabled) return;

            IPlaylist playlist = Library.CurrentPlaylist;
            DateTime currentDateTime = DateTime.Now;
            double position = PlayerPositionMilliseconds;
            double duration = PlayerNaturalDurationMilliseconds;

            playlist.CurrentSong.DurationMilliseconds = duration;

            if (position > 500 && duration > 500) playlist.CurrentSongPositionPercent = position / duration;
            else if (Player?.CurrentState == MediaPlayerState.Playing)
            {
                position = playlist.CurrentSongPositionPercent * playlist.CurrentSong.DurationMilliseconds;
                position += (currentDateTime - previousUpdatedTime).TotalMilliseconds;
                playlist.CurrentSongPositionPercent = position / duration;

                previousUpdatedTime = currentDateTime;
            }
        }

        private void sld_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            playerPositionEnabled = false;
        }

        private void HighestParent_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (playerPositionEnabled) return;

            playerPositionEnabled = true;

            double percent = Library?.CurrentPlaylist?.CurrentSongPositionPercent ?? 0;
            double duration = Library?.CurrentPlaylist?.CurrentSong?.DurationMilliseconds ?? Song.DefaultDuration;

            Player.Position = TimeSpan.FromMilliseconds(percent * duration);
            //Player.Position = Library?.CurrentPlaylist?.GetCurrentSongPosition() ?? new TimeSpan();

            sld.Minimum = 0;
            sld.Maximum = 1;
        }

        private void sld_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Library?.CurrentPlaylist == null) return;

            Library.CurrentPlaylist.CurrentSongPositionPercent = e.NewValue;

            //if (!playerPositionEnabled) return;

            //double duration = Library?.CurrentPlaylist?.CurrentSong?.DurationMilliseconds ?? Song.DefaultDuration;
            //Player.Position = TimeSpan.FromMilliseconds(e.NewValue * duration);
        }

        private void sld_Holding(object sender, HoldingRoutedEventArgs e)
        {
            sld.Minimum = sld.Value - zoomHalfWidth;
            sld.Maximum = sld.Value + zoomHalfWidth;
        }
    }
}
