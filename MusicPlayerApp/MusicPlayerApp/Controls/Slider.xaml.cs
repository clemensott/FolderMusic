using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using FolderMusic.Converters;
using MusicPlayer;
using System.Threading.Tasks;
using FolderMusic.Utils;

namespace FolderMusic
{
    public sealed partial class Slider : UserControl
    {
        private const double zoomWidth = 0.1;
        private static double[] playbackRates = new double[] { 0.5, 0.75, 0.9, 1, 1.15, 1.3, 1.5, 1.75, 2, 2.25, 2.5 };

        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register("IsIndeterminate", typeof(bool),
                typeof(Slider), new PropertyMetadata(false));

        public static readonly DependencyProperty ViewPositionRatioProperty =
            DependencyProperty.Register("ViewPositionRatio", typeof(double), typeof(Slider),
                new PropertyMetadata(0.0, OnViewPositionRatioPropertyChanged));

        private static void OnViewPositionRatioPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Slider s = (Slider)sender;
            double value = (double)e.NewValue;

            if (s.Duration > TimeSpan.Zero) s.ViewPosition = s.Duration.Multiply(value);
            if (s.playerPositionEnabled) s.PositionRatio = value;
        }

        public static readonly DependencyProperty ViewPositionProperty =
            DependencyProperty.Register("ViewPosition", typeof(TimeSpan), typeof(Slider),
                new PropertyMetadata(TimeSpan.Zero, OnViewPositionPropertyChanged));

        private static void OnViewPositionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Slider s = (Slider)sender;
            TimeSpan value = (TimeSpan)e.NewValue;

            if (s.Duration > TimeSpan.Zero) s.ViewPositionRatio = value.TotalDays / s.Duration.TotalDays;
        }

        public static readonly DependencyProperty PositionRatioProperty =
            DependencyProperty.Register("PositionRatio", typeof(double), typeof(Slider),
                new PropertyMetadata(0.0, OnPositionRatioPropertyChanged));

        private static void OnPositionRatioPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Slider s = (Slider)sender;
            double value = (double)e.NewValue;

            if (s.playerPositionEnabled) s.ViewPositionRatio = value;
        }


        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(Slider),
                new PropertyMetadata(TimeSpan.Zero, OnDurationPropertyChanged));

        private static void OnDurationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Slider s = (Slider)sender;
            TimeSpan value = (TimeSpan)e.NewValue;

            if (value > TimeSpan.Zero && s.playerPositionEnabled)
            {
                s.ViewPosition = value.Multiply(s.PositionRatio);
            }
        }

        public static readonly DependencyProperty PlaybackRateProperty = DependencyProperty
            .Register(nameof(PlaybackRate), typeof(double), typeof(Slider), new PropertyMetadata(default(double)));

        private bool playerPositionEnabled = true;
        private int currentTblPlaybackRateTransitionId = 0;

        public bool IsIndeterminate
        {
            get { return (bool)GetValue(IsIndeterminateProperty); }
            set { SetValue(IsIndeterminateProperty, value); }
        }

        public double ViewPositionRatio
        {
            get { return (double)GetValue(ViewPositionRatioProperty); }
            set { SetValue(ViewPositionRatioProperty, value); }
        }

        public TimeSpan ViewPosition
        {
            get { return (TimeSpan)GetValue(ViewPositionProperty); }
            set { SetValue(ViewPositionProperty, value); }
        }

        public double PositionRatio
        {
            get { return (double)GetValue(PositionRatioProperty); }
            set { SetValue(PositionRatioProperty, value); }
        }

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public double PlaybackRate
        {
            get { return (double)GetValue(PlaybackRateProperty); }
            set { SetValue(PlaybackRateProperty, value); }
        }

        public Slider()
        {
            this.InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement highestParent = this;

            while (highestParent.Parent is FrameworkElement) highestParent = (FrameworkElement)highestParent.Parent;

            highestParent.PointerExited += HighestParent_PointerExited;
        }

        private object PlaybackRateConverter_ConvertEvent(object value, Type targetType, object parameter, string language)
        {
            double playbackRate = (double)value;
            return string.Format("{0,2}x", playbackRate);
        }

        private void sld_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            playerPositionEnabled = false;
        }

        private void HighestParent_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (playerPositionEnabled) return;
            playerPositionEnabled = true;

            PositionRatio = ViewPositionRatio;

            sld.Minimum = 0;
            sld.Maximum = 1;
            tblBegin.Visibility = tblEnd.Visibility = Visibility.Collapsed;
        }

        private void sld_Holding(object sender, HoldingRoutedEventArgs e)
        {
            double value = sld.Value;
            sld.Minimum = value - zoomWidth * value;
            sld.Maximum = value + (1 - value) * zoomWidth;

            double min = sld.Minimum;
            double max = sld.Maximum;
            TimeSpan duration = Duration;
            TimeSpan beginTime = duration.Multiply(min);
            TimeSpan endTime = duration.Multiply(max);

            tblBegin.Text = TimeSpanConverter.Convert(beginTime);
            tblEnd.Text = TimeSpanConverter.Convert(endTime);
            tblBegin.Visibility = tblEnd.Visibility = Visibility.Visible;
        }

        private void TblViewPosition_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            int index = playbackRates.IndexOf(PlaybackRate);
            if (index > 0)
            {
                PlaybackRate = playbackRates[index - 1];
            }
        }

        private void TblViewDuration_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            int index = playbackRates.IndexOf(PlaybackRate);
            if (index + 1 < playbackRates.Length)
            {
                PlaybackRate = playbackRates[index + 1];
            }
        }

        private async void TblPlaybackRate_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                double? newPlaybackRate = await PlaybackRateSelectionDialog.Start(playbackRates, PlaybackRate);
                if (newPlaybackRate.HasValue)
                {
                    PlaybackRate = newPlaybackRate.Value;
                }
            }
            catch (Exception exc)
            {
                await new Windows.UI.Popups.MessageDialog(exc.ToString(), "select playback rate error").ShowAsync();
            }
        }
    }
}
