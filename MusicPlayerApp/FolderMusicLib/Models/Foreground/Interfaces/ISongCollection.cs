using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Foreground.Shuffle;

namespace MusicPlayer.Models.Foreground.Interfaces
{
    public interface ISongCollection : IEnumerable<Song>, INotifyPropertyChanged, IXmlSerializable
    {
        int Count { get; }
        IShuffleCollection Shuffle { get; }

        event EventHandler<SongCollectionChangedEventArgs> Changed;
        event EventHandler<ShuffleChangedEventArgs> ShuffleChanged;

        void Remove(Song song);
        void Change(IEnumerable<Song> removes, IEnumerable<Song> adds);
        int IndexOf(Song song);
        void SetShuffleType(ShuffleType type, Song? currentSong);
    }
}