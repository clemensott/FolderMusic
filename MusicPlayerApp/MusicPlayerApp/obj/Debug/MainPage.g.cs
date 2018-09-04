﻿

#pragma checksum "C:\Users\Clemens\Documents\Visual Studio 2015\Projects\MusicPlayerApp\MusicPlayerApp\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "D2F15104B93178F77A4678850F39ED88"
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
    partial class MainPage : global::Windows.UI.Xaml.Controls.Page, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 10 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.FrameworkElement)(target)).Loaded += this.Page_Loaded;
                 #line default
                 #line hidden
                #line 10 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).PointerExited += this.Page_PointerExited;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 149 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).PointerEntered += this.sld_PointerEntered;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 167 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.Loop_Tapped;
                 #line default
                 #line hidden
                #line 168 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).PointerEntered += this.LoopImage_PointerEntered;
                 #line default
                 #line hidden
                #line 168 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).PointerExited += this.LoopImage_PointerExited;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 139 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.Shuffle_Tapped;
                 #line default
                 #line hidden
                #line 140 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).PointerEntered += this.ShuffleImage_PointerEntered;
                 #line default
                 #line hidden
                #line 140 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).PointerExited += this.ShuffleImage_PointerExited;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 63 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Holding += this.PlaylistsPlaylist_Holding;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 71 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target)).Click += this.RefreshPlaylist_Click;
                 #line default
                 #line hidden
                break;
            case 7:
                #line 72 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target)).Click += this.SearchForNewSongsPlaylist_Click;
                 #line default
                 #line hidden
                break;
            case 8:
                #line 73 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target)).Click += this.DeletePlaylist_Click;
                 #line default
                 #line hidden
                break;
            case 9:
                #line 78 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.PlayPlaylist_Tapped;
                 #line default
                 #line hidden
                break;
            case 10:
                #line 80 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.Playlist_Tapped;
                 #line default
                 #line hidden
                break;
            case 11:
                #line 26 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.FrameworkElement)(target)).DataContextChanged += this.lbxCurrentPlaylist_DataContextChanged;
                 #line default
                 #line hidden
                break;
            case 12:
                #line 29 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Holding += this.CurrentPlaylistSong_Holding;
                 #line default
                 #line hidden
                #line 29 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.CurrentPlaylistSong_Tapped;
                 #line default
                 #line hidden
                break;
            case 13:
                #line 37 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target)).Click += this.RefreshSong_Click;
                 #line default
                 #line hidden
                break;
            case 14:
                #line 38 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MenuFlyoutItem)(target)).Click += this.DeleteSong_Click;
                 #line default
                 #line hidden
                break;
            case 15:
                #line 180 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.Previous_Click;
                 #line default
                 #line hidden
                break;
            case 16:
                #line 181 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.PlayPause_Click;
                 #line default
                 #line hidden
                break;
            case 17:
                #line 182 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.Next_Click;
                 #line default
                 #line hidden
                break;
            case 18:
                #line 186 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.RefreshEveryPlaylists_Click;
                 #line default
                 #line hidden
                break;
            case 19:
                #line 187 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.SearchForNewPlaylists_Click;
                 #line default
                 #line hidden
                break;
            case 20:
                #line 188 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.TestFunktion_Click;
                 #line default
                 #line hidden
                break;
            case 21:
                #line 189 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.TestFunktion_Click2;
                 #line default
                 #line hidden
                break;
            case 22:
                #line 190 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.TestFunktion_Click3;
                 #line default
                 #line hidden
                break;
            case 23:
                #line 191 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.TestFunktion_Click4;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


