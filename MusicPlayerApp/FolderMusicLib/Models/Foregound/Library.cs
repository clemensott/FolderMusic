using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;
using MusicPlayer.Models.Skip;

namespace MusicPlayer.Models
{
    public class Library : ILibrary
    {
        private IPlaylist currentPlaylist;

        public event EventHandler<ChangedEventArgs<IPlaylist>> CurrentPlaylistChanged;

        public IPlaylist this[int index] => Playlists.ElementAtOrDefault(index);

        public IPlaylist CurrentPlaylist
        {
            get { return currentPlaylist; }
            set
            {
                if (value == currentPlaylist) return;

                ChangedEventArgs<IPlaylist> args = new ChangedEventArgs<IPlaylist>(currentPlaylist, value);
                currentPlaylist = value;
                CurrentPlaylistChanged?.Invoke(this, args);
                OnPropertyChanged(nameof(CurrentPlaylist));
            }
        }

        public IPlaylistCollection Playlists { get; }

        public SkipSongs SkippedSongs { get; }

        internal Library()
        {
            SkippedSongs = new SkipSongs(this);
            Playlists = new PlaylistCollection();
            Playlists.Changed += Playlists_Changed;
            CurrentPlaylist = null;
        }

        private void Playlists_Changed(object sender, PlaylistCollectionChangedEventArgs e)
        {
            ChangeCollectionItem<IPlaylist> item;
            if (Playlists.Count == 0) CurrentPlaylist = null;
            else if (Playlists.Contains(CurrentPlaylist)) return;
            else if (e.RemovedPlaylists.TryFirst(p => p.Item == CurrentPlaylist, out item))
            {
                CurrentPlaylist = Playlists.ElementAtOrDefault(item.Index) ?? Playlists.Last();
            }
            else CurrentPlaylist = Playlists.First();
        }

        public static async Task<ILibrary> Load(string fileName)
        {
            ILibrary library = new Library();
            try
            {
                string xml = await IO.LoadTextAsync(fileName);
                library.ReadXml(XmlConverter.GetReader(xml));
                //UpdateLibraryUtils.CheckLibrary(library, "Load");
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("Load library error", e, fileName);
            }

            return library;
        }

        public static async Task Save(string fileName, ILibrary library)
        {
            try
            {
                if (library.Playlists.Count > 0) await IO.SaveObjectAsync(fileName, library);
                else await IO.DeleteAsync(fileName);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("Save library error", e, fileName);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string currentPlaylistPath = reader.GetAttribute("CurrentPlaylistPath");

            try
            {
                XmlConverter.Deserialize(Playlists, reader.ReadInnerXml());
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("LibraryXmlLoadFail", e, reader.Name, reader.NodeType);
                throw;
            }

            CurrentPlaylist = Playlists.FirstOrDefault(p => p.AbsolutePath == currentPlaylistPath) ??
                              Playlists.FirstOrDefault();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("CurrentPlaylistPath", CurrentPlaylist?.AbsolutePath ?? "null");

            writer.WriteStartElement("Playlists");
            Playlists.WriteXml(writer);
            writer.WriteEndElement();
        }
    }
}
