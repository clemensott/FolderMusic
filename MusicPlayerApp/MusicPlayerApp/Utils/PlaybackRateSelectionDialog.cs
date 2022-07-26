using FolderMusic.Controls;
using MusicPlayer.Models.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace FolderMusic.Utils
{
    class PlaybackRateSelectionDialog
    {
        private readonly PlaybackRatesListControl list;
        private readonly ContentDialog dialog;

        private PlaybackRateSelectionDialog(IEnumerable<double> playbackRates, double playbackRate)
        {
            list = new PlaybackRatesListControl(playbackRates, playbackRate);
            list.PlaybackRateChanged += List_PlaybackRateChanged;

            dialog = new ContentDialog()
            {
                Content = list,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = "Cancel",
                IsSecondaryButtonEnabled = false,
            };
        }

        private void List_PlaybackRateChanged(object sender, ChangedEventArgs<double> e)
        {
            dialog.Hide();
        }

        private async Task<double?> Start()
        {
            ContentDialogResult result = await dialog.ShowAsync();

            return result == ContentDialogResult.None ? list.SelectedPlaybackRate : null;
        }

        public static Task<double?> Start(IEnumerable<double> playbackRates, double playbackRate)
        {
            PlaybackRateSelectionDialog dialog = new PlaybackRateSelectionDialog(playbackRates, playbackRate);
            return dialog.Start();
        }
    }
}
