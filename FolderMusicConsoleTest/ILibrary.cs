using System.Collections.Generic;

namespace MusicPlayer.Data
{
    internal interface ILibrary
    {
        IEnumerable<IPlaylist> Playlists { get; }
    }

    class Library : ILibrary
    {
        private IPlaylist playlist;

        public IEnumerable<IPlaylist> Playlists
        {
            get
            {
                yield return playlist;
            }
        }

        public Library()
        {
            playlist = new Playlist();
        }
    }
}