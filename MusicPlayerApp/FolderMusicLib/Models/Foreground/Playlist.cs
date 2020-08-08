using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Windows.Storage;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Foreground.Interfaces;

namespace MusicPlayer.Models.Foreground
{
    class Playlist : IPlaylist
    {
        private const string emptyName = "None", emptyOrLoadingPath = "None";

        private TimeSpan position;
        private Song currentSong;
        private LoopType loop;

        public event EventHandler<ChangedEventArgs<Song>> CurrentSongChanged;
        public event EventHandler<ChangedEventArgs<TimeSpan>> PositionChanged;
        public event EventHandler<ChangedEventArgs<LoopType>> LoopChanged;

        public TimeSpan Position
        {
            get { return position; }
            set
            {
                if (value == position) return;

                ChangedEventArgs<TimeSpan> args = new ChangedEventArgs<TimeSpan>(position, value);
                position = value;
                PositionChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(Position));
            }
        }

        public string Name { get; private set; }

        public string AbsolutePath { get; private set; }

        public Song CurrentSong
        {
            get { return currentSong; }
            set
            {
                if (Equals(value, currentSong)) return;

                ChangedEventArgs<Song> args = new ChangedEventArgs<Song>(currentSong, value);
                currentSong = value;
                CurrentSongChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(CurrentSong));
            }
        }

        public ISongCollection Songs { get; }

        public LoopType Loop
        {
            get { return loop; }
            set
            {
                if (value == loop) return;

                ChangedEventArgs<LoopType> args = new ChangedEventArgs<LoopType>(loop, value);
                loop = value;
                LoopChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(Loop));
            }
        }

        public Playlist()
        {
            Name = emptyName;
            AbsolutePath = emptyOrLoadingPath;

            Songs = new SongCollection();
            Songs.Changed += Songs_Changed;
            Loop = LoopType.Off;
        }

        public Playlist(string path) : this()
        {
            Name = path != string.Empty ? Path.GetFileName(path) : KnownFolders.MusicLibrary.Name;
            AbsolutePath = path;
        }

        private void Songs_Changed(object sender, SongCollectionChangedEventArgs e)
        {
            if (Songs.Shuffle.Count == 0) return;

            Song song;
            ChangeCollectionItem<Song> item;

            if (Songs.TryGetSong(CurrentSong.FullPath, out song))
            {
                CurrentSong = song;
            }
            else if (e.RemovedSongs.TryFirst(r => r.Item.FullPath == CurrentSong.FullPath, out item))
            {
                CurrentSong = item.Index < Songs.Shuffle.Count
                    ? Songs.Shuffle.ElementAt(item.Index)
                    : Songs.Shuffle.First();
            }
            else CurrentSong = Songs.Shuffle.First();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public override string ToString()
        {
            return Name;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string rawPosition = reader.GetAttribute(nameof(Position));
            TimeSpan position = string.IsNullOrWhiteSpace(rawPosition)
                ? TimeSpan.Zero
                : TimeSpan.FromTicks(long.Parse(rawPosition));

            AbsolutePath = reader.GetAttribute(nameof(AbsolutePath)) ?? emptyOrLoadingPath;
            Name = reader.GetAttribute(nameof(Name)) ?? emptyName;
            Loop = (LoopType)Enum.Parse(typeof(LoopType), reader.GetAttribute(nameof(Loop)) ?? LoopType.Off.ToString());

            string currentSongPath = reader.GetAttribute(nameof(CurrentSong)) ?? string.Empty;

            ShuffleType shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType),
                reader.GetAttribute(nameof(Shuffle)) ?? ShuffleType.Off.ToString());

            reader.ReadStartElement();

            XmlConverter.Deserialize(Songs, reader.ReadOuterXml());

            Song song;
            CurrentSong = Songs.TryGetSong(currentSongPath, out song) ? song : Songs.Shuffle.FirstOrDefault();
            Position = position;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(AbsolutePath), AbsolutePath);
            writer.WriteAttributeString(nameof(CurrentSong), CurrentSong.FullPath);
            writer.WriteAttributeString(nameof(Position), position.Ticks.ToString());
            writer.WriteAttributeString(nameof(Loop), Loop.ToString());
            writer.WriteAttributeString(nameof(Name), Name);

            writer.WriteStartElement(Songs.GetType().Name);
            Songs.WriteXml(writer);
            writer.WriteEndElement();
        }
    }
}
