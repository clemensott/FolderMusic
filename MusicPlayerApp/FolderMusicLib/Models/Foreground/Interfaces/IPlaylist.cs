using System;
using System.ComponentModel;
using System.Xml.Serialization;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;

namespace MusicPlayer.Models.Foreground.Interfaces
{

    public interface IPlaylist : INotifyPropertyChanged, IXmlSerializable
    {
        event EventHandler<ChangedEventArgs<Song>> CurrentSongChanged;
        event EventHandler<ChangedEventArgs<TimeSpan>> PositionChanged;
        event EventHandler<ChangedEventArgs<LoopType>> LoopChanged;

        string AbsolutePath { get; }
        Song CurrentSong { get; set; }
        TimeSpan Position { get; set; }
        LoopType Loop { get; set; }
        string Name { get; }
        ISongCollection Songs { get; }

        string ToString();
    }
}