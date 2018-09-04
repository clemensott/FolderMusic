using FolderMusic.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace FolderMusic
{
    public sealed partial class IListStringControl : UserControl
    {
        private bool focusTbx;
        private int selectedIndex;
        private IList<string> list;

        public IListStringControl()
        {
            this.InitializeComponent();

            Select(-1, true);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine((sender as Button).ActualWidth + " x " + (sender as Button).ActualHeight);
            if (list == null) return;

            list.Add(string.Empty);

            UpdateUiList(list.Count - 1, true);
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (list == null && selectedIndex > 0) return;

            list.RemoveAt(selectedIndex);

            UpdateUiList(Math.Min(selectedIndex, list.Count - 1), false);
        }

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {
            if (list == null || selectedIndex - 1 < 0) return;

            string item = list[selectedIndex];
            list.RemoveAt(selectedIndex);
            list.Insert(selectedIndex - 1, item);

            UpdateUiList(selectedIndex - 1, false);
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e)
        {
            if (list == null || selectedIndex + 1 >= list.Count) return;

            string item = list[selectedIndex];
            list.RemoveAt(selectedIndex);
            list.Insert(selectedIndex + 1, item);

            UpdateUiList(selectedIndex + 1, false);
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            list = DataContext as IList<string>;

            UpdateUiList(-1, true);
            System.Diagnostics.Debug.WriteLine(list != null);
        }

        private void UpdateUiList()
        {
            UpdateUiList(selectedIndex, focusTbx);
        }

        private void UpdateUiList(int indexToSelect, bool tbxOnFocus)
        {
            int count = list?.Count ?? 0;

            tblNone.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;

            while (panel.Children.Count < count)
            {
                panel.Children.Add(GetDefaultListGrid());
            }

            while (panel.Children.Count > count)
            {
                panel.Children.RemoveAt(panel.Children.Count - 1);
            }

            for (int i = 0; i < count; i++)
            {
                TextBox tbx = GetListUiElement<TextBox>(i);
                TextBlock tbl = GetListUiElement<TextBlock>(i);
                Rectangle rect = GetListUiElement<Rectangle>(i);

                tbx.Text = list[i];
                tbl.Text = list[i];
            }

            Select(indexToSelect, tbxOnFocus);
        }

        private Grid GetDefaultListGrid()
        {
            Grid grid = new Grid();
            grid.Children.Add(GetDefaultListTextBox());
            grid.Children.Add(GetDefaultListRect());
            grid.Children.Add(GetDefaultListTextBlock());

            return grid;
        }

        private TextBox GetDefaultListTextBox()
        {
            TextBox tbx = new TextBox();
            Binding widthBinding = new Binding()
            {
                ElementName = "control",
                Path = new PropertyPath("ActualWidth")
            };

            Binding fontSizeBinding = new Binding()
            {
                ElementName = "control",
                Path = new PropertyPath("FontSize")
            };

            tbx.SetBinding(WidthProperty, widthBinding);
            tbx.SetBinding(FontSizeProperty, fontSizeBinding);

            tbx.TextChanged += Tbx_TextChanged;
            tbx.LostFocus += Tbx_LostFocus;

            return tbx;
        }

        private TextBlock GetDefaultListTextBlock()
        {
            TextBlock tbl = new TextBlock();
            Binding fontSizeBinding = new Binding()
            {
                ElementName = "control",
                Path = new PropertyPath("FontSize")
            };

            tbl.SetBinding(FontSizeProperty, fontSizeBinding);

            tbl.MinWidth = 30;
            tbl.HorizontalAlignment = HorizontalAlignment.Left;

            tbl.Tapped += Tbl_Tapped;
            tbl.GotFocus += Tbl_GotFocus;

            return tbl;
        }

        private Rectangle GetDefaultListRect()
        {
            Rectangle rect = new Rectangle();
            Binding widthBinding = new Binding()
            {
                ElementName = "control",
                Path = new PropertyPath("ActualWidth")
            };

            rect.SetBinding(WidthProperty, widthBinding);

            rect.Fill = new SolidColorBrush(Colors.Transparent);

            rect.Tapped += Rect_Tapped;
            rect.DoubleTapped += Rect_DoubleTapped;

            return rect;
        }

        private void Tbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tbx = sender as TextBox;

            int index = GetListUiElementIndex(tbx);
            TextBlock tbl = GetListUiElement<TextBlock>(index);

            if (index != -1) list[index] = tbx.Text;
            if (tbl != null) tbl.Text = tbx.Text;
        }

        private void Tbx_LostFocus(object sender, RoutedEventArgs e)
        {
            int index = GetListUiElementIndex(sender as UIElement);

            if (index == selectedIndex) Select(selectedIndex, false);
        }

        private void Tbl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(e.GetPosition(sender as UIElement));
            //System.Diagnostics.Debug.WriteLine((sender as FrameworkElement).Width +
            //    ": " + (sender as FrameworkElement).ActualWidth);

            Select(GetListUiElementIndex(sender as UIElement), true);
        }

        private void Tbl_GotFocus(object sender, RoutedEventArgs e)
        {
            Select(GetListUiElementIndex(sender as UIElement), false);
        }

        private void Rect_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Select(GetListUiElementIndex(sender as UIElement), false);
        }

        private void Rect_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Select(GetListUiElementIndex(sender as UIElement), true);
        }

        private void Select(int index, bool focusOnTbx)
        {
            selectedIndex = index;
            focusTbx = focusOnTbx;

            int count = list?.Count ?? 0;

            for (int i = 0; i < count; i++)
            {
                TextBox tbx = GetListUiElement<TextBox>(i);
                TextBlock tbl = GetListUiElement<TextBlock>(i);
                Rectangle rect = GetListUiElement<Rectangle>(i);

                UpdateSelection(tbx, tbl, rect, i == selectedIndex);
            }

            if (selectedIndex == -1) Focus(FocusState.Pointer);

            btnRemove.IsEnabled = count > 0;
            btnUp.IsEnabled = count > 1 && selectedIndex > 0;
            btnDown.IsEnabled = count > 1 && selectedIndex != -1 && selectedIndex + 1 < count;
        }

        private void UpdateSelection(TextBox tbx, TextBlock tbl, Rectangle rect, bool isSelected)
        {
            if (isSelected)
            {
                if (focusTbx)
                {
                    tbx.Visibility = Visibility.Visible;
                    rect.Visibility = tbl.Visibility = Visibility.Collapsed;

                    rect.Fill = GetTransparentBrush();

                    tbx.Focus(FocusState.Keyboard);
                }
                else
                {
                    tbx.Visibility = Visibility.Collapsed;
                    rect.Visibility = tbl.Visibility = Visibility.Visible;

                    rect.Fill = GetSelectedBrush();
                }
            }
            else
            {
                tbx.Visibility = Visibility.Collapsed;
                rect.Visibility = tbl.Visibility = Visibility.Visible;

                rect.Fill = GetTransparentBrush();
            }
        }

        private Brush GetTransparentBrush()
        {
            return new SolidColorBrush(Colors.Transparent);
        }

        private Brush GetSelectedBrush()
        {
            return (Brush)Resources["ListBoxItemSelectedBackgroundThemeBrush"];
        }

        private T GetListUiElement<T>(int index) where T : class
        {
            return panel.Children.OfType<Grid>().ElementAtOrDefault(index)?.Children.OfType<T>().FirstOrDefault();
        }

        private int GetListUiElementIndex(UIElement element)
        {
            int index = 0;
            Type type = element?.GetType();
            IEnumerable<UIElement> allElements = panel.Children.OfType<Grid>().SelectMany(g => g.Children);

            foreach (UIElement child in allElements.Where(c => c.GetType().Equals(type)))
            {
                if (child == element) return index;

                index++;
            }

            return -1;
        }
    }
}
