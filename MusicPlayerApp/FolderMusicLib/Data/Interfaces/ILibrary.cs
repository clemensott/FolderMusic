using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MusicPlayer.Data
{
    public delegate void PlayStateChangedEventHandler(ILibrary sender, PlayStateChangedEventArgs args);
    public delegate void LibraryChangedEventHandler(ILibrary sender, LibraryChangedEventsArgs args);
    public delegate void PlaylistsPropertyChangedEventHandler(ILibrary sender, PlaylistsChangedEventArgs args);
    public delegate void CurrentPlaylistPropertyChangedEventHandler(ILibrary sender, CurrentPlaylistChangedEventArgs args);
    public delegate void SettingsPropertyChangedEventHandler();

    public interface ILibrary : IXmlSerializable
    {
        event PlayStateChangedEventHandler PlayStateChanged;
        event LibraryChangedEventHandler LibraryChanged;
        event PlaylistsPropertyChangedEventHandler PlaylistsChanged;
        event CurrentPlaylistPropertyChangedEventHandler CurrentPlaylistChanged;
        event SettingsPropertyChangedEventHandler SettingsChanged;

        IPlaylist this[int index] { get; }

        bool CanceledLoading { get; }
        IPlaylist CurrentPlaylist { get; set; }
        bool IsLoadedComplete { get; }
        bool IsPlaying { get; set; }
        IPlaylistCollection Playlists { get; }
        SkipSongs SkippedSongs { get; }

        Task AddNew();
        void CancelLoading();
        void LoadComplete();
        Task Refresh();
        void Save();
        Task SaveAsync();
        void Set(ILibrary library);
        Task Update();
    }
}