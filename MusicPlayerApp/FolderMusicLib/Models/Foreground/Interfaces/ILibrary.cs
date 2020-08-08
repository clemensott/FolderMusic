using System;
using System.ComponentModel;
using System.Xml.Serialization;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Skip;

namespace MusicPlayer.Models.Foreground.Interfaces
{
    public interface ILibrary : INotifyPropertyChanged, IXmlSerializable
    {
        event EventHandler<ChangedEventArgs<IPlaylist>> CurrentPlaylistChanged;

        IPlaylist this[int index] { get; }

        IPlaylist CurrentPlaylist { get; set; }
        IPlaylistCollection Playlists { get; }
        SkipSongs SkippedSongs { get; }
    }
}