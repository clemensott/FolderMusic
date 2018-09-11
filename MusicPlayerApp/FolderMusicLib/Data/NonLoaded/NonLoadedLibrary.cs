using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace MusicPlayer.Data.NonLoaded
{
    class NonLoadedLibrary : ILibrary
    {
        private const string nonLoadedFileName = "SimpleData.xml";

        public event PlayStateChangedEventHandler PlayStateChanged;
        public event LibraryChangedEventHandler LibraryChanged;
        public event PlaylistsPropertyChangedEventHandler PlaylistsChanged;
        public event CurrentPlaylistPropertyChangedEventHandler CurrentPlaylistChanged;
        public event SettingsPropertyChangedEventHandler SettingsChanged;

        private bool isPlaying;

        public IPlaylist this[int index] { get { return Playlists.ElementAtOrDefault(index); } }

        public bool CanceledLoading { get; private set; }

        public IPlaylist CurrentPlaylist { get; set; }

        public bool IsPlaying
        {
            get { return isPlaying; }
            set
            {
                if (value == isPlaying) return;

                isPlaying = value;
                var args = new PlayStateChangedEventArgs(value);
                PlayStateChanged?.Invoke(this, args);
            }
        }

        public IPlaylistCollection Playlists { get; private set; }

        public SkipSongs SkippedSongs { get; private set; }

        public bool IsLoadedComplete { get { return false; } }

        private NonLoadedLibrary(string xmlText)
        {
            SkippedSongs = new SkipSongs(this);
            IsPlaying = false;
            ReadXml(XmlConverter.GetReader(xmlText));
        }

        public NonLoadedLibrary(ILibrary actualLibrary)
        {
            string currentPlaylistPath = actualLibrary.CurrentPlaylist.AbsolutePath;

            Playlists = new NonLoadedPlaylistCollection(this, actualLibrary.Playlists, actualLibrary.CurrentPlaylist);
            CurrentPlaylist = Playlists.FirstOrDefault(p => p.AbsolutePath == currentPlaylistPath) ?? Playlists.FirstOrDefault();
        }

        public static ILibrary Load()
        {
            return new NonLoadedLibrary(IO.LoadText(nonLoadedFileName));
        }

        public async Task AddNew()
        {
        }

        public void CancelLoading()
        {
        }

        public async Task Refresh()
        {
        }

        public void Save()
        {
            try
            {
                IO.SaveObject(nonLoadedFileName, this);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("NonLoadedLibrarySaveFail", e);
            }
        }

        public async Task SaveAsync()
        {
            await new Task(new Action(Save));
        }

        public async Task Update()
        {
        }

        public void Set(ILibrary library)
        {
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string currentPlaylistPath = reader.GetAttribute("CurrentPlaylistPath");

            Playlists = new NonLoadedPlaylistCollection(this, reader.ReadInnerXml());

            CurrentPlaylist = Playlists.FirstOrDefault(p => p.AbsolutePath == currentPlaylistPath) ?? Playlists.FirstOrDefault();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("CurrentPlaylistPath", CurrentPlaylist.AbsolutePath);

            writer.WriteStartElement("Playlists");
            Playlists.WriteXml(writer);
            writer.WriteEndElement();
        }

        public void LoadComplete()
        {
        }
    }
}
