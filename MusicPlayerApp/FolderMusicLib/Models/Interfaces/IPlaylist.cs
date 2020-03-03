using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MusicPlayer.Models.EventArgs;

namespace MusicPlayer.Models.Interfaces
{
    public enum LoopType { Off, All, Current }

    public interface IPlaylist : INotifyPropertyChanged, IXmlSerializable
    {
        event EventHandler<CurrentSongChangedEventArgs> CurrentSongChanged;
        event EventHandler<CurrentSongPositionChangedEventArgs> CurrentSongPositionChanged;
        event EventHandler<LoopChangedEventArgs> LoopChanged;
        event EventHandler<SongsChangedEventArgs> SongsChanged;

        string AbsolutePath { get; }
        Song CurrentSong { get; set; }
        double CurrentSongPosition { get; set; }
        LoopType Loop { get; set; }
        string Name { get; }
        IPlaylistCollection Parent { get; set; }
        ISongCollection Songs { get; set; }

        Task Reset(StopOperationToken stopToken);
        Task ResetSongs(StopOperationToken stopToken);
        Task AddNew(StopOperationToken stopToken);
        IPlaylist ToSimple();
        string ToString();
        Task Update(StopOperationToken stopToken);
    }
}