using System;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkID=390556 dokumentiert.

namespace MobileDebug
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class DebugPage : Page
    {
        private static readonly TimeSpan minHoldingTimeSpan = TimeSpan.FromMilliseconds(300);

        private bool isHolding;
        private ViewModelDebug viewModel;

        public DebugPage()
        {
            this.InitializeComponent();

            viewModel = ViewModelDebug.GetInstance(lbxEvents.SelectedItems);
            DataContext = viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            viewModel.RestoreSelectedItems();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            viewModel.StoreSelectedItems();
        }

        private void Ref_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Reload();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await FileIO.WriteTextAsync(await Service.GetBackDebugDataFile(), string.Empty);
                await FileIO.WriteTextAsync(await Service.GetForeDebugDataFile(), string.Empty);
            }
            catch { }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DebugFilterPage), viewModel);
        }

        private void AbtbFind_Holding(object sender, HoldingRoutedEventArgs e)
        {
            Frame.Navigate(typeof(DebugFilterPage), viewModel);
        }

        private void ShowBackground_Click(object sender, RoutedEventArgs e)
        {
            ScrollToFirstSelectedItem();
        }

        private void ShowForeground_Click(object sender, RoutedEventArgs e)
        {
            ScrollToFirstSelectedItem();
        }

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            ScrollToFirstSelectedItem();
        }

        private void ScrollToFirstSelectedItem()
        {
            try
            {
                if (lbxEvents.SelectedItems.Count == 0) return;

                object firstSelectedItem = lbxEvents.Items.First(i => lbxEvents.SelectedItems.Contains(i));
                lbxEvents.ScrollIntoView(firstSelectedItem);
            }
            catch { }
        }
    }
}
