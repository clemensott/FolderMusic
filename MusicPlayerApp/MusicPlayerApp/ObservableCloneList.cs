using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FolderMusic
{
    public class ObservableCloneList<T> : ObservableCollection<T>
    {
        private IList<T> clone;

        public ObservableCloneList(IList<T> list) : base(list)
        {
            clone = list;
        }

        protected override void ClearItems()
        {
            clone.Clear();
            base.ClearItems();
        }

        protected override void InsertItem(int index, T item)
        {
            clone.Insert(index, item);
            base.InsertItem(index, item);
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            T item = clone[oldIndex];
            clone.RemoveAt(oldIndex);
            clone.Insert(newIndex, item);

            base.MoveItem(oldIndex, newIndex);
        }

        protected override void RemoveItem(int index)
        {
            clone.RemoveAt(index);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, T item)
        {
            clone[index] = item;
            base.SetItem(index, item);
        }
    }
}

//using FolderMusic.Converters;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;

//// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

//namespace FolderMusic
//{
//    public sealed partial class IListStringControl : UserControl
//    {
//        private int selectedIndex = -1;
//        private IList<string> list;
//        private IsNotLastConverter notLastCon;

//        public IListStringControl()
//        {
//            this.InitializeComponent();

//            notLastCon = (IsNotLastConverter)Resources["notLastCon"];
//        }

//        private void BtnAdd_Click(object sender, RoutedEventArgs e)
//        {
//            if (list == null) return;

//            list.Add("Text");
//            notLastCon.Count = list.Count;

//            if (list.Count > 0) lbx.Visibility = Visibility.Visible;

//            lbx.SelectedIndex = list.Count - 1;
//        }

//        private void BtnRemove_Click(object sender, RoutedEventArgs e)
//        {
//            if (list == null) return;

//            list.Remove(lbx.SelectedItem as string);
//            notLastCon.Count = list.Count;

//            if (list.Count == 0) lbx.Visibility = Visibility.Collapsed;
//        }

//        private void BtnUp_Click(object sender, RoutedEventArgs e)
//        {
//            int index = lbx.SelectedIndex;

//            if (list == null || index + 1 >= list.Count) return;

//            list.Move(index, index - 1);
//        }

//        private void BtnDown_Click(object sender, RoutedEventArgs e)
//        {
//            int index = lbx.SelectedIndex;

//            if (list == null || index - 1 >= 0) return;

//            list.Move(index, index + 1);
//        }

//        //Width="{Binding ElementName=control,Path=ActualWidth}"/>

//        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
//        {
//            IList<string> iList = DataContext as IList<string>;

//            if (iList != null)
//            {
//                ObservableCollection<string> collection = iList as ObservableCollection<string>;

//                if (collection != null) lbx.ItemsSource = list = collection;
//                else lbx.ItemsSource = list = new ObservableCloneList<string>(iList);

//                notLastCon.Count = list.Count;

//                lbx.Visibility = list.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

//            }

//            System.Diagnostics.Debug.WriteLine(list != null);
//        }

//        private void lbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            Unfocus(selectedIndex);

//            selectedIndex = lbx.SelectedIndex;

//            Focus(selectedIndex);
//        }

//        private void Focus(int index)
//        {
//            TextBox tbx;
//            TextBlock tbl;

//            if (!TryGetUiElements(index, out tbx, out tbl)) return;

//            tbx.Visibility = Visibility.Visible;
//            tbl.Visibility = Visibility.Collapsed;

//            tbl.Focus(FocusState.Unfocused);
//            tbx.Focus(FocusState.Keyboard);
//        }

//        private void Unfocus(int index)
//        {
//            TextBox tbx;
//            TextBlock tbl;

//            if (!TryGetUiElements(index, out tbx, out tbl)) return;

//            tbl.Visibility = Visibility.Visible;
//            tbx.Visibility = Visibility.Collapsed;

//            tbl.Focus(FocusState.Unfocused);
//            tbx.Focus(FocusState.Unfocused);
//        }

//        private bool TryGetUiElements(int index, out TextBox tbx, out TextBlock tbl)
//        {
//            tbx = null;
//            tbl = null;

//            if (index == -1) return false;

//            try
//            {
//                ListBoxItem item = lbx.ContainerFromIndex(index) as ListBoxItem;
//                Grid grid = item?.Content as Grid;

//                tbx = grid?.Children[0] as TextBox;
//                tbl = grid?.Children[1] as TextBlock;
//            }
//            catch (Exception e) { }

//            return tbx != null && tbl != null;
//        }
//    }
//}


<!--<UserControl
    x:Class="FolderMusic.IListStringControl"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:FolderMusic"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:con="using:FolderMusic.Converters"
    mc:Ignorable="d"
    d:DesignHeight="300"
    d:DesignWidth="400"
    x:Name="control"
    DataContextChanged="OnDataContextChanged">

    <UserControl.Resources>
        <con:IsNotNullConverter x:Key="notNullCon"/>
        <con:IsNotFirstConverter x:Key="notFirstCon"/>
        <con:IsNotLastConverter x:Key="notLastCon"/>
    </UserControl.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Text="Empty" Margin="5,0,0,0"/>

        <ListBox x:Name="lbx" SelectionMode="Single" Background="Transparent" Margin="0,-10"
                 SelectionChanged="lbx_SelectionChanged">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <Grid>
                        <TextBox Text="{Binding Mode=TwoWay}" Visibility="Collapsed"
                                 Width="{Binding ElementName=control,Path=ActualWidth}"/>
                        <TextBlock Text="{Binding}" Visibility="Visible"/>
                    </Grid>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <StackPanel Grid.Row="1" Orientation="Horizontal">
            <Button Content="Add" Margin="5,5,0,0" Width="75" Click="BtnAdd_Click"/>

            <Button Content="Remove" Margin="5,5,0,0" Width="75" Click="BtnRemove_Click"
                    IsEnabled="{Binding ElementName=lbx,Path=SelectedItem,
                      Converter={StaticResource notNullCon}}"/>

            <Button Content="Up" Margin="5,5,0,0" Width="75" Click="BtnUp_Click"
                    IsEnabled="{Binding ElementName=lbx,Path=SelectedIndex,
                      Converter={StaticResource notFirstCon}}"/>

            <Button Content="Down" Margin="5,5,0,0" Width="75" Click="BtnDown_Click"
                    IsEnabled="{Binding ElementName=lbx,Path=SelectedIndex,
                      Converter={StaticResource notLastCon}}"/>
        </StackPanel>
    </Grid>
</UserControl>-->