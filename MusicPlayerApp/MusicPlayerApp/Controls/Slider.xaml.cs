using System;
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

        public static readonly DependencyProperty ViewPositionRatioProperty =
            DependencyProperty.Register("ViewPositionRatio", typeof(double), typeof(Slider),
                new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnViewPositionRatioPropertyChanged)));

        private static void OnViewPositionRatioPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var value = (double)e.NewValue;

            s.ViewPosition = TimeSpan.FromDays(value * s.Duration.TotalDays);
        }

        public static readonly DependencyProperty ViewPositionProperty =
            DependencyProperty.Register("ViewPosition", typeof(TimeSpan), typeof(Slider),
                new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnViewPositionPropertyChanged)));

        private static void OnViewPositionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var value = (TimeSpan)e.NewValue;

            if (s.Duration > TimeSpan.Zero) s.ViewPositionRatio = value.TotalDays / s.Duration.TotalDays;
        }

        public static readonly DependencyProperty PositionRatioProperty =
            DependencyProperty.Register("PositionRatio", typeof(double), typeof(Slider),
                new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnPositionRatioPropertyChanged)));

        private static void OnPositionRatioPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var value = (double)e.NewValue;

            s.ViewPositionRatio = value;
            s.Position = TimeSpan.FromDays(value * s.Duration.TotalDays);
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(TimeSpan), typeof(Slider),
                new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnPositionPropertyChanged)));

        private static void OnPositionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var value = (TimeSpan)e.NewValue;

            s.ViewPosition = value;

            if (s.Duration > TimeSpan.Zero) s.PositionRatio = value.TotalDays / s.Duration.TotalDays;
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(Slider),
                new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnDurationPropertyChanged)));

        private static void OnDurationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Slider)sender;
            var value = (TimeSpan)e.NewValue;

            s.Position = TimeSpan.FromDays(s.PositionRatio * value.TotalDays);
        }

        private bool playerPositionEnabled = true;

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

        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public Slider()
        {
            this.InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement highestParent = this;

            while (highestParent.Parent as FrameworkElement != null) highestParent = highestParent.Parent as FrameworkElement;

            highestParent.PointerExited += HighestParent_PointerExited;
        }

        private void sld_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            playerPositionEnabled = false;
        }

        private void HighestParent_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (playerPositionEnabled) return;

            playerPositionEnabled = true;

            Position = ViewPosition;

            sld.Minimum = 0;
            sld.Maximum = 1;
        }

        private void sld_Holding(object sender, HoldingRoutedEventArgs e)
        {
            double value = sld.Value;
            sld.Minimum = value - zoomWidth * value;
            sld.Maximum = value + (1 - value) * zoomWidth;
        }
    }
}
