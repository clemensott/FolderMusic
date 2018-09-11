using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace MusicPlayer.Data.NonLoaded
{
    class NonLoadedPlaylist : IPlaylist
    {
        private const string defaultName = "None";
        private double currentSongPositionPercent;
        private Song currentSong;
        private LoopType loop;
        private ShuffleType shuffle;

        public event CurrentSongPropertyChangedEventHandler CurrentSongChanged;
        public event CurrentSongPositionPropertyChangedEventHandler CurrentSongPositionChanged;
        public event ShufflePropertyChangedEventHandler ShuffleChanged;
        public event LoopPropertyChangedEventHandler LoopChanged;

        public Song this[int index] { get { return Songs.ElementAtOrDefault(index); } }

        public string AbsolutePath { get; private set; }

        public Song CurrentSong
        {
            get { return currentSong; }
            set
            {
                //if (value == currentSong || !Songs.Contains(value)) return;

                currentSong = value;
            }
        }

        public double CurrentSongPositionPercent
        {
            get { return currentSongPositionPercent; }
            set
            {
                if (value == currentSongPositionPercent) return;

                currentSongPositionPercent = value;
            }
        }

        //public bool IsEmpty { get { return false; } }

        public LoopType Loop { get; set; }

        public string Name { get; private set; }

        public IPlaylistCollection Parent { get; set; }

        public ShuffleType Shuffle { get; set; }

        public IShuffleCollection ShuffleSongs { get; private set; }

        public int SongsCount { get; private set; }

        public ISongCollection Songs { get; private set; }

        public NonLoadedPlaylist(IPlaylistCollection parent, IPlaylist actualPlaylist, bool isCurrentPlaylist)
        {
            Parent = parent;

            AbsolutePath = actualPlaylist.AbsolutePath;
            Name = actualPlaylist.Name;
            SongsCount = actualPlaylist.SongsCount;
            Loop = actualPlaylist.Loop;
            Shuffle = actualPlaylist.Shuffle;

            if (isCurrentPlaylist)
            {
                CurrentSong = actualPlaylist.CurrentSong;
                Songs = new NonLoadedSongCollection(this, actualPlaylist.ShuffleSongs);
            }
            else
            {
                Songs = new NonLoadedSongCollection(this);
                CurrentSong = Song.GetEmpty(Songs);
            }
        }

        public NonLoadedPlaylist(IPlaylistCollection parent, CurrentPlaySong currentPlaySong)
        {
            Parent = parent;

            AbsolutePath = Name = defaultName;
            SongsCount = 1;
            Loop = LoopType.Off;
            Shuffle = ShuffleType.Off;

            Songs = new NonLoadedSongCollection(this, currentPlaySong);
            ShuffleSongs = new NonLoadedShuffleCollection(this, Songs, Shuffle);

            currentSong = Songs.First();
            currentSongPositionPercent = currentPlaySong.PositionPercent;
        }

        public NonLoadedPlaylist(IPlaylistCollection parent, string xmlText)
        {
            Parent = parent;
            ReadXml(XmlConverter.GetReader(xmlText));
        }

        public async Task Reset()
        {
        }

        public async Task ResetSongs()
        {
        }

        public async Task AddNew()
        {
        }

        public void SetNextLoop()
        {
        }

        public void SetNextShuffle()
        {
        }

        public void SetShuffle(IShuffleCollection shuffleSongs)
        {
        }

        public void SetNextSong()
        {
            ChangeCurrentSong(1);
        }

        public void SetPreviousSong()
        {
            ChangeCurrentSong(-1);
        }

        public void ChangeCurrentSong(int offset)
        {

        }

        public async Task Update()
        {
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            AbsolutePath = reader.GetAttribute("AbsolutePath") ?? defaultName;
            currentSongPositionPercent = double.Parse(reader.GetAttribute("CurrentSongPositionPercent") ?? "0");
            Name = reader.GetAttribute("Name") ?? defaultName;
            Loop = (LoopType)Enum.Parse(typeof(LoopType), reader.GetAttribute("Loop") ?? LoopType.Off.ToString());
            Shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType), reader.GetAttribute("Shuffle") ?? ShuffleType.Off.ToString());
            SongsCount = int.Parse(reader.GetAttribute("SongsCount") ?? "0");

            string currentSongPath = reader.GetAttribute("CurrentSongPath") ?? string.Empty;

            reader.ReadStartElement();
            Songs = new NonLoadedSongCollection(this, reader.ReadOuterXml());
            ShuffleSongs = new NonLoadedShuffleCollection(this, Songs, Shuffle);

            CurrentSong = Songs.FirstOrDefault(s => s.Path == currentSongPath) ?? Songs.FirstOrDefault();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("AbsolutePath", AbsolutePath);
            writer.WriteAttributeString("CurrentSongPath", CurrentSong.Path);
            writer.WriteAttributeString("CurrentSongPositionPercent", currentSongPositionPercent.ToString());
            writer.WriteAttributeString("Loop", Loop.ToString());
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Shuffle", Shuffle.ToString());
            writer.WriteAttributeString("SongsCount", SongsCount.ToString());

            writer.WriteStartElement("Songs");
            Songs.WriteXml(writer);
            writer.WriteEndElement();
        }
    }
}
