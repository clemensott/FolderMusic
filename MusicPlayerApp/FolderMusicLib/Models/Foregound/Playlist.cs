using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Windows.Storage;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;
using MusicPlayer.Models.Enums;

namespace MusicPlayer.Models
{
    class Playlist : IPlaylist
    {
        private const string emptyName = "None", emptyOrLoadingPath = "None";

        private TimeSpan currentSongPosition;
        private Song currentSong;
        private ISongCollection songs;
        private LoopType loop = LoopType.Off;

        public event EventHandler<ChangedEventArgs<Song>> CurrentSongChanged;
        public event EventHandler<ChangedEventArgs<TimeSpan>> PositionChanged;
        public event EventHandler<ChangedEventArgs<LoopType>> LoopChanged;
        public event EventHandler<SongsChangedEventArgs> SongsChanged;

        public TimeSpan Position
        {
            get { return currentSongPosition; }
            set
            {
                if (value == currentSongPosition) return;

                ChangedEventArgs<TimeSpan> args = new ChangedEventArgs<TimeSpan>(currentSongPosition, value);
                currentSongPosition = value;
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

        public ISongCollection Songs
        {
            get { return songs; }
            set
            {
                if (value == songs) return;

                if (songs != null) songs.Changed += Songs_Changed;
                SongsChangedEventArgs args = new SongsChangedEventArgs(songs, value);
                songs?.Shuffle?.Dispose();
                songs = value;
                if (songs != null) songs.Changed += Songs_Changed;

                SongsChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(Songs));
            }
        }

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
            Songs = new SongCollection();

            Name = emptyName;
            AbsolutePath = emptyOrLoadingPath;

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
            string currentSongPath = CurrentSong.FullPath;

            if (Songs.TryFirst(s => s.FullPath == currentSongPath, out song))
            {
                CurrentSong = song;
            }
            else if (e.RemovedSongs.TryFirst(r => r.Item.FullPath == currentSongPath, out item))
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
            string rawCurrentSongPosition = reader.GetAttribute("CurrentSongPosition") ?? "0";
            TimeSpan currentSongPosition = TimeSpan.FromTicks(long.Parse(rawCurrentSongPosition));

            AbsolutePath = reader.GetAttribute("AbsolutePath") ?? emptyOrLoadingPath;
            Name = reader.GetAttribute("Name") ?? emptyName;
            Loop = (LoopType)Enum.Parse(typeof(LoopType), reader.GetAttribute("Loop") ?? LoopType.Off.ToString());

            string currentSongPath = reader.GetAttribute("CurrentSongPath") ?? string.Empty;

            ShuffleType shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType),
                reader.GetAttribute("Shuffle") ?? ShuffleType.Off.ToString());

            reader.ReadStartElement();

            Songs = XmlConverter.Deserialize(new SongCollection(), reader.ReadOuterXml());

            Song song;
            CurrentSong = Songs.TryFirst(s => s.FullPath == currentSongPath, out song) ? song : songs.FirstOrDefault();
            Position = currentSongPosition;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("AbsolutePath", AbsolutePath);
            writer.WriteAttributeString("CurrentSongPath", CurrentSong.FullPath);
            writer.WriteAttributeString("CurrentSongPosition", currentSongPosition.Ticks.ToString());
            writer.WriteAttributeString("Loop", Loop.ToString());
            writer.WriteAttributeString("Name", Name);

            writer.WriteStartElement(Songs.GetType().Name);
            Songs.WriteXml(writer);
            writer.WriteEndElement();
        }
    }
}
