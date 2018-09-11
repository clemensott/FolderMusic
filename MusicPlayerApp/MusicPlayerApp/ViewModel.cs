using MusicPlayer.Data;
using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using PlayerIcons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace FolderMusic
{
    public class ViewModel : INotifyPropertyChanged
    {
        private SymbolIcon playIcon, pauseIcon;

        public bool IsPlaying { get { return Library.IsPlaying; } }

        public IconElement PlayPauseIcon { get { return Library.IsPlaying ? GetPauseIcon() : GetPlayIcon(); } }

        public ILibrary Library { get; private set; }

        public IPlaylist CurrentPlaylist { get { return Library.CurrentPlaylist; } }

        public IEnumerable<IPlaylist> Playlists { get { return Library.Playlists; } }

        public string CurrentPlaylistName { get { return CurrentPlaylist?.Name ?? "Empty"; } }

        public BitmapImage CurrentPlaylistShuffleIcon { get { return GetCurrentPlaylistShuffleIcon(); } }

        public BitmapImage CurrentPlaylistLoopIcon { get { return GetCurrentPlaylistLoopIcon(); } }

        public MediaPlayer BackgroundPlayer { get { return BackgroundMediaPlayer.Current; } }

        public string CurrentSongTitle { get { return CurrentPlaylist?.CurrentSong?.Title ?? string.Empty; } }

        public string CurrentSongArtist { get { return CurrentPlaylist?.CurrentSong?.Artist ?? string.Empty; } }

        public ViewModel(ILibrary library)
        {
            Library = library;

            library.LibraryChanged += OnLibraryChanged;
            library.PlayStateChanged += OnPlayStateChanged;
            library.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            library.PlaylistsChanged += OnPlaylistsChanged;

            Subscribe(library.Playlists);
            Subscribe(library.CurrentPlaylist);
        }

        private void Subscribe(IPlaylistCollection playlists)
        {
            playlists.Changed += OnPlaylistCollectionChanged;

            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                //playlist.Songs.CollectionChanged += Songs_CollectionChanged;
            }
        }

        private void Unsubscribe(IPlaylistCollection playlists)
        {
            playlists.Changed -= OnPlaylistCollectionChanged;

            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                //playlist.Songs.CollectionChanged -= Songs_CollectionChanged;
            }
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.LoopChanged += OnLoopChanged;
            playlist.ShuffleChanged += OnShuffleChanged;

            Subscribe(playlist.ShuffleSongs);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.ShuffleChanged -= OnShuffleChanged;

            Unsubscribe(playlist.ShuffleSongs);
        }

        private void Subscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>()) Subscribe(song);
        }

        private void Unsubscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>()) Unsubscribe(song);
        }

        private void Subscribe(Song song)
        {
            song.ArtistChanged += OnArtistChanged;
            song.TitleChanged += OnTitleChanged;
        }

        private void Unsubscribe(Song song)
        {
            song.ArtistChanged -= OnArtistChanged;
            song.TitleChanged -= OnTitleChanged;
        }

        private SymbolIcon GetPlayIcon()
        {
            try
            {
                if (playIcon == null) playIcon = new SymbolIcon(Symbol.Play);
            }
            catch
            {
                return new SymbolIcon(Symbol.Play);
            }

            return playIcon;
        }

        private SymbolIcon GetPauseIcon()
        {
            try
            {
                if (pauseIcon == null) pauseIcon = new SymbolIcon(Symbol.Pause);
            }
            catch
            {
                return new SymbolIcon(Symbol.Pause);
            }

            return pauseIcon;
        }

        private string GetShowTime(double totalMilliseconds)
        {
            try
            {
                int totalSeconds = Convert.ToInt32(totalMilliseconds / 1000);
                int seconds = totalSeconds % 60, minutes = (totalSeconds / 60) % 60, hours = totalSeconds / 3600;
                string time = string.Empty;

                time += hours > 0 ? hours.ToString() + ":" : string.Empty;
                time += hours > 0 ? string.Format("{0,2}", minutes) : minutes.ToString();
                time += string.Format(":{0,2}", seconds);

                return time.Replace(" ", "0");
            }
            catch { }

            return "Catch";
        }

        private BitmapImage GetCurrentPlaylistShuffleIcon()
        {
            switch (CurrentPlaylist?.Shuffle ?? ShuffleType.Off)
            {
                case ShuffleType.Complete:
                    return Icons.Current.ShuffleComplete;

                case ShuffleType.OneTime:
                    return Icons.Current.ShuffleOneTime;

                default:
                    return Icons.Current.ShuffleOff;
            }
        }

        private BitmapImage GetCurrentPlaylistLoopIcon()
        {
            switch (CurrentPlaylist?.Loop ?? LoopType.Off)
            {
                case LoopType.All:
                    return Icons.Current.LoopAll;

                case LoopType.Current:
                    return Icons.Current.LoopCurrent;

                default:
                    return Icons.Current.LoopOff;
            }
        }

        public void UpdatePlayPauseIconAndText()
        {
            NotifyPropertyChanged("IsPlaying");
            NotifyPropertyChanged("PlayPauseIcon");
            NotifyPropertyChanged("PlayPauseText");
        }

        public void UpdatePlaylists()
        {
            NotifyPropertyChanged("Playlists");
        }

        public void UpdateCurrentPlaylistAndRest()
        {
            NotifyPropertyChanged("CurrentPlaylist");

            NotifyPropertyChanged("CurrentPlaylistName");
            NotifyPropertyChanged("CurrentPlaylistLoop");
            NotifyPropertyChanged("CurrentPlaylistShuffleIcon");

            UpdateCurrentSongTitleAndArtist();
        }

        public void UpdateCurrentSongTitleAndArtist()
        {
            NotifyPropertyChanged("CurrentSongTitle");
            NotifyPropertyChanged("CurrentSongArtist");
        }

        private void OnLibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            Unsubscribe(args.OldPlaylists);
            Subscribe(args.NewPlaylists);

            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            UpdatePlayPauseIconAndText();
            UpdatePlaylists();
            UpdateCurrentPlaylistAndRest();
            UpdateCurrentSongTitleAndArtist();
        }

        private void OnPlaylistsChanged(ILibrary sender, PlaylistsChangedEventArgs args)
        {
            Unsubscribe(args.OldPlaylists);
            Subscribe(args.NewPlaylists);

            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            UpdatePlaylists();
            UpdateCurrentPlaylistAndRest();
        }

        private void OnPlaylistCollectionChanged(IPlaylistCollection sender, PlaylistCollectionChangedEventArgs args)
        {
            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            UpdatePlaylists();
            UpdateCurrentPlaylistAndRest();
        }

        private void OnCurrentPlaylistChanged(ILibrary sender, CurrentPlaylistChangedEventArgs args)
        {
            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            UpdateCurrentPlaylistAndRest();
        }


        private void OnSkippedSongsChanged(SkipSongs sender)
        {
            //if (sender.MoveNext()) (Window.Current.Content as Frame).Navigate(typeof(SkipSongsPage));
        }

        private void OnCurrentSongChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            UpdateCurrentSongTitleAndArtist();
        }

        private void OnShuffleChanged(IPlaylist sender, ShuffleChangedEventArgs args)
        {
            NotifyPropertyChanged("CurrentPlaylistShuffleIcon");
        }

        private void OnLoopChanged(IPlaylist sender, LoopChangedEventArgs args)
        {
            NotifyPropertyChanged("CurrentPlaylistLoopIcon");
        }

        private void OnTitleChanged(Song sender, SongTitleChangedEventArgs args)
        {
            if (sender == CurrentPlaylist.CurrentSong) NotifyPropertyChanged("CurrentSongTitle");
        }

        private void OnArtistChanged(Song sender, SongArtistChangedEventArgs args)
        {
            if (sender == CurrentPlaylist.CurrentSong) NotifyPropertyChanged("CurrentSongArtist");
        }

        private void OnPlayStateChanged(ILibrary sender, PlayStateChangedEventArgs args)
        {
            UpdatePlayPauseIconAndText();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                if (null == PropertyChanged) return;

                MainPage.DoSafe(() => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("ViewModelNotifyFail", e);
            }
        }
    }
}
