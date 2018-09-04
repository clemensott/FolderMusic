using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Data
{
    public class Feedback
    {
        private static Feedback instance;

        public static Feedback Current
        {
            get
            {
                if (instance == null) instance = new Feedback();

                return instance;
            }
        }

        private Feedback()
        {
            FolderMusicDebug.DebugEvent.SaveText("CreateFeedback");
        }

        public delegate void OnLibraryChangedHandler(ILibrary sender, LibraryChangedEventsArgs args);
        public delegate void OnPlaylistsPropertyChangedHandler(ILibrary sender, PlaylistsChangedEventArgs args);
        public delegate void OnCurrentPlaylistPropertyChangedHandler(ILibrary sender, CurrentPlaylistChangedEventArgs args);
        public delegate void OnSkippedSongsPropertyChangedHandler(SkipSongs sender);
        public delegate void OnSongsPropertyChangedHandler(Playlist sender, SongsChangedEventArgs args);
        public delegate void OnCurrentSongPropertyChangedHandler(Playlist sender, CurrentSongChangedEventArgs args);
        public delegate void OnCurrentSongPositionPropertyChangedHandler(Playlist sender, CurrentSongPositionChangedEventArgs args);
        public delegate void OnShufflePropertyChangedHandler(Playlist sender, ShuffleChangedEventArgs args);
        public delegate void OnLoopPropertyChangedHandler(Playlist sender, LoopChangedEventArgs args);
        public delegate void OnTitlePropertyChangedHandler(Song sender, SongTitleChangedEventArgs args);
        public delegate void OnArtistPropertyChangedHandler(Song sender, SongArtistChangedEventArgs args);
        public delegate void OnNaturalDurationPropertyChangedHandler(Song sender, SongNaturalDurationChangedEventArgs args);
        public delegate void OnSettingsPropertyChangedHandler();
        public delegate void OnPlayStateChangedHandler(ILibrary sender, PlayStateChangedEventArgs args);

        public event OnLibraryChangedHandler OnLibraryChanged;
        public event OnPlaylistsPropertyChangedHandler OnPlaylistsPropertyChanged;
        public event OnCurrentPlaylistPropertyChangedHandler OnCurrentPlaylistPropertyChanged;
        public event OnSkippedSongsPropertyChangedHandler OnSkippedSongsPropertyChanged;
        public event OnSongsPropertyChangedHandler OnSongsPropertyChanged;
        public event OnCurrentSongPropertyChangedHandler OnCurrentSongPropertyChanged;
        public event OnCurrentSongPositionPropertyChangedHandler OnCurrentSongPositionPropertyChanged;
        public event OnShufflePropertyChangedHandler OnShufflePropertyChanged;
        public event OnLoopPropertyChangedHandler OnLoopPropertyChanged;
        public event OnTitlePropertyChangedHandler OnTitlePropertyChanged;
        public event OnArtistPropertyChangedHandler OnArtistPropertyChanged;
        public event OnNaturalDurationPropertyChangedHandler OnNaturalDurationPropertyChanged;
        public event OnSettingsPropertyChangedHandler OnSettingsPropertyChanged;
        public event OnPlayStateChangedHandler OnPlayStateChanged;

        internal void RaiseLibraryChanged(ILibrary oldLibrary, ILibrary newLibrary)
        {
            Library.Current.SaveAsync();

            LibraryChangedEventsArgs args = new LibraryChangedEventsArgs(oldLibrary, newLibrary);
            OnLibraryChanged?.Invoke(newLibrary, args);
        }

        internal void RaisePlaylistsPropertyChanged(ChangedPlaylist[] addPlaylists, ChangedPlaylist[] removePlaylists,
            Playlist oldCurrentPlaylist, Playlist newCurrentPlaylist)
        {
            Library.Current.SaveAsync();

            PlaylistsChangedEventArgs args = new PlaylistsChangedEventArgs
                (addPlaylists, removePlaylists, oldCurrentPlaylist, newCurrentPlaylist);

            OnPlaylistsPropertyChanged?.Invoke(Library.Current, args);
        }

        internal void RaisePlaylistsPropertyChanged(PlaylistList oldPlaylists, PlaylistList newPlaylists,
            Playlist oldCurrentPlaylist, Playlist newCurrentPlaylist)
        {
            Library.Current.SaveAsync();

            PlaylistsChangedEventArgs args = new PlaylistsChangedEventArgs
                (oldPlaylists, newPlaylists, oldCurrentPlaylist, newCurrentPlaylist);

            OnPlaylistsPropertyChanged?.Invoke(Library.Current, args);
        }

        internal void RaiseCurrentPlaylistPropertyChanged(ILibrary sender, Playlist oldCurrentPlaylist, Playlist newCurrentPlaylist)
        {
            CurrentPlaylistChangedEventArgs args = new CurrentPlaylistChangedEventArgs(oldCurrentPlaylist, newCurrentPlaylist);
            OnCurrentPlaylistPropertyChanged?.Invoke(sender, args);
        }

        internal void RaisePlayStateChanged(ILibrary sender, bool newPlayState)
        {
            PlayStateChangedEventArgs args = new PlayStateChangedEventArgs(newPlayState);
            OnPlayStateChanged?.Invoke(sender, args);
        }

        internal void RaiseSkippedSongsPropertyChanged()
        {
            OnSkippedSongsPropertyChanged?.Invoke(Library.Current.SkippedSongs);
        }

        internal void RaiseSongsPropertyChanged(Playlist sender, ChangedSong[] add, ChangedSong[] remove,
            ShuffleType oldShuffleType, ShuffleType newShuffleType, List<int> oldShuffleList,
            List<int> newShuffleList, Song oldCurrentSong, Song newCurrentSong)
        {
            Library.Current.SaveAsync();

            SongsChangedEventArgs args = new SongsChangedEventArgs(add, remove, oldShuffleType, newShuffleType,
                oldShuffleList, newShuffleList, oldCurrentSong, newCurrentSong);

            OnSongsPropertyChanged?.Invoke(sender, args);
        }

        internal void RaiseSongsPropertyChanged(Playlist sender, IList<Song> oldSongs, IList<Song> newSongs,
            ShuffleType oldShuffleType, ShuffleType newShuffleType, List<int> oldShuffleList,
            List<int> newShuffleList, Song oldCurrentSong, Song newCurrentSong)
        {
            Library.Current.SaveAsync();

            SongsChangedEventArgs args = new SongsChangedEventArgs(oldSongs, newSongs, oldShuffleType,
                newShuffleType, oldShuffleList, newShuffleList, oldCurrentSong, newCurrentSong);

            OnSongsPropertyChanged?.Invoke(sender, args);
        }

        internal void RaiseCurrentSongPropertyChanged(Playlist sender, Song oldCurrentSong, Song newCurrentSong)
        {
            CurrentSongChangedEventArgs args = new CurrentSongChangedEventArgs(oldCurrentSong, newCurrentSong);

            object[] targets = OnCurrentSongPropertyChanged.GetInvocationList().Select(l => l.Target).ToArray();
            FolderMusicDebug.DebugEvent.SaveText("RaiseCurrentSong", (object[])targets);
            OnCurrentSongPropertyChanged?.Invoke(sender, args);
            FolderMusicDebug.DebugEvent.SaveText("RaisedCurrentSong", targets.GroupBy(t => t).Count());
        }

        internal void RaiseCurrentSongPositionPropertyChanged(Playlist sender, double oldPosition, double newPosition)
        {
            CurrentSongPositionChangedEventArgs args = new CurrentSongPositionChangedEventArgs(oldPosition, newPosition);

            OnCurrentSongPositionPropertyChanged?.Invoke(sender, args);
        }

        internal void RaiseShufflePropertyChanged(Playlist sender, ShuffleType oldShuffleType, ShuffleType newShuffleType,
            List<int> oldShuffleList, List<int> newShuffleList, Song oldCurrentSong, Song newCurrentSong)
        {
            Library.Current.SaveAsync();

            ShuffleChangedEventArgs args = new ShuffleChangedEventArgs(oldShuffleType, newShuffleType,
                oldShuffleList, newShuffleList, oldCurrentSong, newCurrentSong);

            OnShufflePropertyChanged?.Invoke(sender, args);
        }

        internal void RaiseLoopPropertyChanged(Playlist sender, LoopType oldLoopType, LoopType newLoopType)
        {
            Library.Current.SaveAsync();

            LoopChangedEventArgs args = new LoopChangedEventArgs(oldLoopType, newLoopType);

            OnLoopPropertyChanged?.Invoke(sender, args);
        }

        internal void RaiseTitlePropertyChanged(Song sender, string oldTitle, string newTitle)
        {
            if (!Library.IsLoaded(sender)) return;

            SongTitleChangedEventArgs args = new SongTitleChangedEventArgs(oldTitle, newTitle);

            OnTitlePropertyChanged?.Invoke(sender, args);
        }

        internal void RaiseArtistPropertyChanged(Song sender, string oldArtist, string newArtist)
        {
            if (!Library.IsLoaded(sender)) return;

            SongArtistChangedEventArgs args = new SongArtistChangedEventArgs(oldArtist, newArtist);

            OnArtistPropertyChanged?.Invoke(sender, args);
        }

        internal void RaiseNaturalDurationPropertyChanged(Song sender, double oldDuration, double newDuration)
        {
            if (!Library.IsLoaded(sender)) return;

            SongNaturalDurationChangedEventArgs args = new SongNaturalDurationChangedEventArgs(oldDuration, newDuration);

            OnNaturalDurationPropertyChanged?.Invoke(sender, args);
        }

        public void RaiseSettingsPropertyChanged()
        {
            OnSettingsPropertyChanged?.Invoke();
        }
    }
}
