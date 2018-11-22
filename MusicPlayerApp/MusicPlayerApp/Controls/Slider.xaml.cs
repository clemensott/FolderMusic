using FolderMusic.ViewModels;
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
        private const double intervall = 100, zoomWidth = 0.1;

        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register("IsIndeterminate", typeof(bool), typeof(Slider),
                new PropertyMetadata(false, new PropertyChangedCallback(OnIsIndeterminatePropertyChanged)));

        private static void OnIsIndeterminatePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var value = (bool)e.NewValue;
        }

        public static readonly DependencyProperty CurrentSongProperty =
            DependencyProperty.Register("CurrentSong", typeof(CurrentSongViewModel), typeof(Slider),
                new PropertyMetadata(null, new PropertyChangedCallback(OnCurrentSongPropertyChanged)));

        private static void OnCurrentSongPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var value = (CurrentSongViewModel)e.NewValue;
        }

        public static readonly DependencyProperty PlayerProperty =
            DependencyProperty.Register("Player", typeof(MediaPlayer), typeof(Slider),
                new PropertyMetadata(null, new PropertyChangedCallback(OnPlayerPropertyChanged)));

        private static void OnPlayerPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            MobileDebug.Service.WriteEvent("SliderPlayerChanged1");
            var s = (Slider)sender;
            var oldValue = (MediaPlayer)e.OldValue;
            var newValue = (MediaPlayer)e.NewValue;
            MobileDebug.Service.WriteEvent("SliderPlayerChanged2", oldValue?.CurrentState, newValue?.CurrentState);
            if (oldValue != null) oldValue.CurrentStateChanged -= s.MediaPlayer_CurrentStateChanged;
            if (newValue != null) newValue.CurrentStateChanged += s.MediaPlayer_CurrentStateChanged;

            s.SetTimer();
            s.SetIsIndeterminate();
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
                return Player?.Position.TotalMilliseconds ?? Library.DefaultSongsPositionMillis;
            }
        }

        public double PlayerNaturalDurationMilliseconds
        {
            get
            {
                return Player?.NaturalDuration.TotalMilliseconds ?? Song.DefaultDuration;
            }
        }

        public CurrentSongViewModel CurrentSong
        {
            get { return (CurrentSongViewModel)GetValue(CurrentSongProperty); }
            set { SetValue(CurrentSongProperty, value); }
        }

        public MediaPlayer Player
        {
            get { return (MediaPlayer)GetValue(PlayerProperty); }
            set { SetValue(PlayerProperty, value); }
        }

        public Slider()
        {
            this.InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(intervall);
            timer.Tick += Timer_Tick;

            Window.Current.Activated += Window_Activated;

            try
            {
                MobileDebug.Service.WriteEvent("SliderCtor1", Player);
                Player = BackgroundMediaPlayer.Current;
                MobileDebug.Service.WriteEvent("SliderCtor2", Player);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SliderCtorFail", e);
            }

            //Timer_Tick(timer, null);
            timer.Start();

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

        private void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            MobileDebug.Service.WriteEvent("SliderCurrentStateChanged", Player?.CurrentState, CurrentSong?.Title, CurrentSong.Duration);
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
            MediaPlayerState state = Player?.CurrentState ?? MediaPlayerState.Closed;
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

        private void Timer_Tick(object sender, object args)
        {
            //try
            //{
            //    timer.Stop();
            //    timer.Interval = TimeSpan.FromMilliseconds(intervall - (PlayerPositionMilliseconds % intervall));
            //    timer.Start();
            //}
            //catch(Exception e)
            //{
            //    MobileDebug.Service.WriteEvent("SliderTimerTickFail1", e);
            //    MobileDebug.Service.WriteEvent("SliderTimerTickFail2", PlayerPositionMilliseconds);
            //    timer.Start();
            //    MobileDebug.Service.WriteEvent("SliderTimerTickFail3");
            //}

            if (!playerPositionEnabled || CurrentSong == null) return;

            DateTime currentDateTime = DateTime.Now;
            double position = PlayerPositionMilliseconds;
            double duration = PlayerNaturalDurationMilliseconds;

            CurrentSong.Duration = TimeSpan.FromMilliseconds(duration);

            if (position > 500 && duration > 500) CurrentSong.PositionRatio = position / duration;
            else if (Player?.CurrentState == MediaPlayerState.Playing)
            {
                CurrentSong.Position += currentDateTime - previousUpdatedTime;

                previousUpdatedTime = currentDateTime;
            }

            SetIsIndeterminate();
        }

        private void sld_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            playerPositionEnabled = false;
        }

        private void HighestParent_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (playerPositionEnabled) return;

            playerPositionEnabled = true;

            Player.Position = CurrentSong.Position;

            sld.Minimum = 0;
            sld.Maximum = 1;
        }

        private void sld_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (CurrentSong != null) CurrentSong.PositionRatio = e.NewValue;
        }

        private void sld_Holding(object sender, HoldingRoutedEventArgs e)
        {
            double value = sld.Value;
            sld.Minimum = value - zoomWidth * value;
            sld.Maximum = value + (1 - value) * zoomWidth;
        }
    }
}
