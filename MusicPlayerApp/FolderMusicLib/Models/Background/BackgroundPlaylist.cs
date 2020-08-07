using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;

namespace MusicPlayer.Models.Background
{
    public class BackgroundPlaylist
    {
        private Song[] songs;
        private LoopType loop;

        public event EventHandler<ChangedEventArgs<Song[]>> SongsChanged;
        public event EventHandler<ChangedEventArgs<LoopType>> LoopChanged;

        public Song[] Songs
        {
            get { return songs; }
            set
            {
                if (value == null || value.BothNullOrSequenceEqual(songs)) return;

                ChangedEventArgs<Song[]> args = new ChangedEventArgs<Song[]>(songs, value);
                songs = value;
                SongsChanged?.Invoke(this, args);
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
            }
        }

        public BackgroundPlaylist()
        {
            songs = new Song[0];
        }

        public bool TryNext(Song? currentSong, out Song? newCurrentSong)
        {
            int index = currentSong.HasValue ? Songs.IndexOf(currentSong.Value) + 1 : 0;
            if (index >= Songs.Length) index = 0;

            newCurrentSong = Songs.Length > 0 ? (Song?)Songs[index] : null;
            return index > 0;
        }

        public Song? Previous(Song? currentSong)
        {
            if (Songs.Length == 0) return null;

            int index = currentSong.HasValue ? Songs.IndexOf(currentSong.Value) : -1;
            if (index == -1) return Songs[0];
            if (index == 0) return Songs[Songs.Length - 1];

            return Songs[index - 1];
        }
    }
}
