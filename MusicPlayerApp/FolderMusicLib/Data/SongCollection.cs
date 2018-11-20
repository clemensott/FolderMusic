using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using MusicPlayer.Data.Shuffle;
using MusicPlayer.Data.Simple;

namespace MusicPlayer.Data
{
    public class SongCollection : ISongCollection
    {
        private List<Song> list;

        public event EventHandler<SongCollectionChangedEventArgs> Changed;
        public event EventHandler<ShuffleChangedEventArgs> ShuffleChanged;

        public int Count { get { return list.Count; } }

        public IPlaylist Parent { get; set; }

        private IShuffleCollection shuffle;

        public IShuffleCollection Shuffle
        {
            get { return shuffle; }
            set
            {
                if (value == shuffle) return;

                var args = new ShuffleChangedEventArgs(shuffle, value);
                shuffle = value;
                ShuffleChanged?.Invoke(this, args);
            }
        }

        public SongCollection()
        {
            list = new List<Song>();
            Shuffle = new ShuffleOffCollection(this);
        }

        public SongCollection(IEnumerable<Song> songs, ShuffleType type, Song currentSong)
        {
            list = new List<Song>(songs);
            Shuffle = GetShuffleType(type, currentSong);
        }

        public int IndexOf(Song song)
        {
            return list.IndexOf(song);
        }

        public void Add(Song song)
        {
            Change(null, Utils.RepeatOnce(song));
        }

        public void Remove(Song song)
        {
            Change(Utils.RepeatOnce(song), null);
        }

        public void Change(IEnumerable<Song> removes, IEnumerable<Song> adds)
        {
            Song oldCurrentSong, newCurrentSong;
            newCurrentSong = oldCurrentSong = Parent.CurrentSong;
            int currentSongIndex = list.IndexOf(oldCurrentSong);

            Song[] removeArray = removes?.ToArray() ?? new Song[0];
            Song[] addArray = adds?.ToArray() ?? new Song[0];
            Song[] newList = list.Except(removeArray).Concat(addArray).ToArray();

            ChangeCollectionItem<Song>[] removed = ChangeCollectionItem<Song>.GetRemovedChanged(removeArray, list).ToArray();
            ChangeCollectionItem<Song>[] added = ChangeCollectionItem<Song>.GetAddedChanged(addArray, list).ToArray();

            if (removed.Length == 0 && added.Length == 0) return;

            if (!newList.Contains(oldCurrentSong))
            {
                if (currentSongIndex < 0) currentSongIndex = 0;
                if (currentSongIndex >= newList.Length) currentSongIndex = newList.Length - 1;

                newCurrentSong = list.ElementAtOrDefault(currentSongIndex);
            }

            foreach (ChangeCollectionItem<Song> change in removed) list.Remove(change.Item);
            foreach (ChangeCollectionItem<Song> change in added)
            {
                change.Item.Parent = this;
                list.Insert(change.Index, change.Item);
            }

            var args = new SongCollectionChangedEventArgs(added, removed);
            Changed?.Invoke(this, args);

            Parent.CurrentSong = newCurrentSong;
        }

        public ISongCollection ToSimple()
        {
            return new SimpleSongCollection(Shuffle, Parent.CurrentSong);
        }

        public IEnumerator<Song> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            ShuffleType shuffleType = (ShuffleType)Enum.Parse(typeof(ShuffleType),
                reader.GetAttribute("Shuffle") ?? Enum.GetName(typeof(ShuffleType), ShuffleType.Off));
            IShuffleCollection shuffle = GetShuffleType(shuffleType);

            reader.ReadStartElement();
            list = XmlConverter.DeserializeList<Song>(reader, "Song").ToList();

            foreach (Song song in list) song.Parent = this;

            shuffle.ReadXml(XmlConverter.GetReader(reader.ReadOuterXml()));
            Shuffle = shuffle;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Shuffle", Enum.GetName(typeof(ShuffleType), Shuffle.Type));

            foreach (Song song in this)
            {
                writer.WriteStartElement("Song");
                song.WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("Shuffle");
            Shuffle.WriteXml(writer);
            writer.WriteEndElement();
        }

        public void SetShuffleType(ShuffleType type)
        {
            if (type == Shuffle.Type) return;

            Shuffle = GetShuffleType(type, Parent?.CurrentSong);
        }

        private IShuffleCollection GetShuffleType(ShuffleType type)
        {
            switch (type)
            {
                case ShuffleType.Complete:
                    return new ShuffleCompleteCollection(this);

                case ShuffleType.Off:
                    return new ShuffleOffCollection(this);

                case ShuffleType.OneTime:
                    return new ShuffleOneTimeCollection(this);
            }

            throw new NotImplementedException("Value \"" + type + "\"of LoopType is not implemented in GetShuffleType");
        }

        private IShuffleCollection GetShuffleType(ShuffleType type, Song currentSong)
        {
            switch (type)
            {
                case ShuffleType.Complete:
                    return new ShuffleCompleteCollection(this, currentSong);

                case ShuffleType.Off:
                    return new ShuffleOffCollection(this);

                case ShuffleType.OneTime:
                    return new ShuffleOneTimeCollection(this, currentSong);
            }

            throw new NotImplementedException("Value \"" + type + "\"of LoopType is not implemented in GetShuffleType");
        }
    }
}
