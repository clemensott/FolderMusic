﻿

#pragma checksum "E:\Clemens\Dokumente\Visual Studio 2015\Projects\MusicPlayerApp\MusicPlayerApp\PlaylistPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "33BABE13812AA3A275A5352611222BD0"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MusicPlayerApp
{
    partial class PlaylistPage : global::Windows.UI.Xaml.Controls.Page, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 10 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.FrameworkElement)(target)).Loaded += this.Page_Loaded;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 99 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.Shuffle_Tapped;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 100 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.Loop_Tapped;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 61 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.FrameworkElement)(target)).DataContextChanged += this.LbxShuffleSongs_DataContextChanged;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 64 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Holding += this.CurrentPlaylistSong_Holding;
                 #line default
                 #line hidden
                #line 64 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.ShuffleSong_Tapped;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 72 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target)).Click += this.RefreshSong_Click;
                 #line default
                 #line hidden
                break;
            case 7:
                #line 73 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target)).Click += this.DeleteSong_Click;
                 #line default
                 #line hidden
                break;
            case 8:
                #line 26 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.FrameworkElement)(target)).DataContextChanged += this.LbxDefaultSongs_DataContextChanged;
                 #line default
                 #line hidden
                break;
            case 9:
                #line 29 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Holding += this.CurrentPlaylistSong_Holding;
                 #line default
                 #line hidden
                #line 29 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.DefaultSong_Tapped;
                 #line default
                 #line hidden
                break;
            case 10:
                #line 37 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target)).Click += this.RefreshSong_Click;
                 #line default
                 #line hidden
                break;
            case 11:
                #line 38 "..\..\PlaylistPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target)).Click += this.DeleteSong_Click;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


