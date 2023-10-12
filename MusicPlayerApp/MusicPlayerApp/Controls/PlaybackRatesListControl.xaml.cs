using MusicPlayer.Models.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace FolderMusic.Controls
{
    public sealed partial class PlaybackRatesListControl : UserControl
    {
        public event EventHandler<ChangedEventArgs<double>> PlaybackRateChanged;

        public double? SelectedPlaybackRate => lbx.SelectedItem as double?;

        public PlaybackRatesListControl(IEnumerable<double> playbackRates, double playbackRate)
        {
            this.InitializeComponent();

            lbx.ItemsSource = playbackRates;
            lbx.SelectedItem = playbackRate;
            lbx.SelectionChanged += Lvw_SelectionChanged;
        }

        private void Lvw_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null)
            {
                double oldPlaybackRate = e.RemovedItems == null || e.RemovedItems.Count == 0 ? 1d : (double)e.RemovedItems.First();
                double newPlaybackRate = (double)e.AddedItems.First();

                PlaybackRateChanged?.Invoke(this, new ChangedEventArgs<double>(oldPlaybackRate, newPlaybackRate));
            }
        }
    }
}
