using MusicPlayer.Data;
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

        public string CurrentPlaylistName { get { return CurrentPlaylist?.Name ?? "Empty"; } }

        public BitmapImage CurrentPlaylistShuffleIcon { get { return GetCurrentPlaylistShuffleIcon(); } }

        public BitmapImage CurrentPlaylistLoopIcon { get { return GetCurrentPlaylistLoopIcon(); } }

        public MediaPlayer BackgroundPlayer { get { return BackgroundMediaPlayer.Current; } }

        public string CurrentSongTitle { get { return CurrentPlaylist?.CurrentSong?.Title ?? string.Empty; } }

        public string CurrentSongArtist { get { return CurrentPlaylist?.CurrentSong?.Artist ?? string.Empty; } }

        public ViewModel(ILibrary library)
        {
            Library = library;

            library.PlayStateChanged += OnPlayStateChanged;

            if (!library.IsLoaded) library.Loaded += OnLibraryLoaded;
            else
            {
                library.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
                library.PlaylistsChanged += OnPlaylistsChanged;
            }

            Subscribe(library.CurrentPlaylist);
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.LoopChanged += OnLoopChanged;
            playlist.SongsChanged += OnSongsChanged;
            playlist.Songs.ShuffleChanged += OnShuffleChanged;

            Subscribe(playlist.CurrentSong);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.SongsChanged += OnSongsChanged;
            playlist.Songs.ShuffleChanged += OnShuffleChanged;

            Unsubscribe(playlist.CurrentSong);
        }

        private void Subscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged += OnArtistChanged;
            song.TitleChanged += OnTitleChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (song == null) return;

            song.ArtistChanged -= OnArtistChanged;
            song.TitleChanged -= OnTitleChanged;
        }

        private SymbolIcon GetPlayIcon()
        {
            try
            {
                if (playIcon == null) playIcon = new SymbolIcon(Symbol.Play);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("GetPlayIconFail", e);
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
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("GetPauseIconFail", e);
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
            switch (CurrentPlaylist?.Songs?.Shuffle.Type ?? ShuffleType.Off)
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

        private void OnLibraryLoaded(object sender, EventArgs args)
        {
            Library.Loaded -= OnLibraryLoaded;
            Library.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            Library.PlaylistsChanged += OnPlaylistsChanged;

            Subscribe(Library.CurrentPlaylist);

            UpdatePlayPauseIconAndText();
            UpdatePlaylists();
            UpdateCurrentPlaylistAndRest();
            UpdateCurrentSongTitleAndArtist();
        }

        private void OnPlaylistsChanged(object sender, PlaylistsChangedEventArgs args)
        {
            UpdatePlaylists();
            UpdateCurrentPlaylistAndRest();
        }

        private void OnCurrentPlaylistChanged(object sender, CurrentPlaylistChangedEventArgs args)
        {
            Unsubscribe(args.OldCurrentPlaylist);
            Subscribe(args.NewCurrentPlaylist);

            UpdateCurrentPlaylistAndRest();
        }


        private void OnSkippedSongsChanged(SkipSongs sender)
        {
            //if (sender.MoveNext()) (Window.Current.Content as Frame).Navigate(typeof(SkipSongsPage));
        }

        private void OnCurrentSongChanged(object sender, CurrentSongChangedEventArgs args)
        {
            UpdateCurrentSongTitleAndArtist();
        }

        private void OnShuffleChanged(object sender, ShuffleChangedEventArgs args)
        {
            NotifyPropertyChanged("CurrentPlaylistShuffleIcon");
        }

        private void OnLoopChanged(object sender, LoopChangedEventArgs args)
        {
            NotifyPropertyChanged("CurrentPlaylistLoopIcon");
        }

        private void OnSongsChanged(object sender, SongsChangedEventArgs e)
        {
            e.OldSongs.ShuffleChanged -= OnShuffleChanged;

            NotifyPropertyChanged("CurrentPlaylistShuffleIcon");
        }

        private void OnTitleChanged(object sender, SongTitleChangedEventArgs args)
        {
            NotifyPropertyChanged("CurrentSongTitle");
        }

        private void OnArtistChanged(object sender, SongArtistChangedEventArgs args)
        {
            NotifyPropertyChanged("CurrentSongArtist");
        }

        private void OnPlayStateChanged(object sender, PlayStateChangedEventArgs args)
        {
            UpdatePlayPauseIconAndText();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            try
            {
                if (null == PropertyChanged) return;

                Utils.DoSafe(() => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("ViewModelNotifyFail", e);
            }
        }
    }
}
