using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Media.Playback;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Skip;

namespace MusicPlayer.Models.Interfaces
{
    public interface ILibrary : INotifyPropertyChanged, IXmlSerializable
    {
        event EventHandler<IsPlayingChangedEventArgs> IsPlayingChanged;
        event EventHandler<PlayerStateChangedEventArgs> PlayerStateChanged;
        event EventHandler<PlaylistsChangedEventArgs> PlaylistsChanged;
        event EventHandler<CurrentPlaylistChangedEventArgs> CurrentPlaylistChanged;
        event EventHandler SettingsChanged;
        event EventHandler Loaded;

        IPlaylist this[int index] { get; }

        IPlaylist CurrentPlaylist { get; set; }
        bool IsForeground { get; }
        bool IsLoaded { get; }
        bool IsPlaying { get; set; }
        MediaPlayerState PlayerState { get; set; }
        IPlaylistCollection Playlists { get; set; }
        SkipSongs SkippedSongs { get; }

        void BeginCommunication();
        Task AddNew(StopOperationToken stopToken);
        Task Reset(StopOperationToken stopToken);
        Task ResetSongs(StopOperationToken stopToken);
        void Load(IEnumerable<IPlaylist> playlists);
        ILibrary ToSimple();
        Task Update(StopOperationToken stopToken);
    }
}