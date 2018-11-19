using MusicPlayer.Data.Shuffle;
using System;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MusicPlayer.Data
{
    public enum LoopType { Off, All, Current }

    public interface IPlaylist : IXmlSerializable
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

        Task Reset();
        Task ResetSongs();
        Task AddNew();
        IPlaylist ToSimple();
        string ToString();
        Task Update();
    }
}