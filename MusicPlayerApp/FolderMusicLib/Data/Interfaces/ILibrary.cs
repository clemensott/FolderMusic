using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MusicPlayer.Data
{
    public interface ILibrary : IXmlSerializable
    {
        event EventHandler<PlayStateChangedEventArgs> PlayStateChanged;
        event EventHandler<PlaylistsChangedEventArgs> PlaylistsChanged;
        event EventHandler<CurrentPlaylistChangedEventArgs> CurrentPlaylistChanged;
        event EventHandler SettingsChanged;
        event EventHandler Loaded;

        IPlaylist this[int index] { get; }

        bool CanceledLoading { get; }
        IPlaylist CurrentPlaylist { get; set; }
        bool IsForeground { get; }
        bool IsLoaded { get; }
        bool IsPlaying { get; set; }
        IPlaylistCollection Playlists { get; set; }
        SkipSongs SkippedSongs { get; }
        Task AddNew();
        void CancelLoading();
        Task Reset();
        Task ResetSongs();
        void Load(IEnumerable<IPlaylist> playlists);
        ILibrary ToSimple();
        Task Update();
    }
}