using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MusicPlayer.Data
{
    public delegate void CurrentSongPropertyChangedEventHandler(IPlaylist sender, CurrentSongChangedEventArgs args);
    public delegate void CurrentSongPositionPropertyChangedEventHandler(IPlaylist sender, CurrentSongPositionChangedEventArgs args);
    public delegate void ShufflePropertyChangedEventHandler(IPlaylist sender, ShuffleChangedEventArgs args);
    public delegate void LoopPropertyChangedEventHandler(IPlaylist sender, LoopChangedEventArgs args);

    public interface IPlaylist : IXmlSerializable
    {
        event CurrentSongPropertyChangedEventHandler CurrentSongChanged;
        event CurrentSongPositionPropertyChangedEventHandler CurrentSongPositionChanged;
        event ShufflePropertyChangedEventHandler ShuffleChanged;
        event LoopPropertyChangedEventHandler LoopChanged;

        Song this[int index] { get; }

        string AbsolutePath { get; }
        Song CurrentSong { get; set; }
        double CurrentSongPositionPercent { get; set; }
        LoopType Loop { get; set; }
        string Name { get; }
        IPlaylistCollection Parent { get; set; }
        ShuffleType Shuffle { get; set; }
        IShuffleCollection ShuffleSongs { get; }
        int SongsCount { get; }
        ISongCollection Songs { get; }

        void ChangeCurrentSong(int offset);
        Task Reset();
        Task ResetSongs();
        Task AddNew();
        void SetNextLoop();
        void SetNextShuffle();
        void SetNextSong();
        void SetPreviousSong();
        void SetShuffle(IShuffleCollection shuffleSongs);
        string ToString();
        Task Update();
    }
}