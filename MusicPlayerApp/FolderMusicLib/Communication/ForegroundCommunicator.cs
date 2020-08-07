using System;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using MusicPlayer.Communication.Messages;
using MusicPlayer.Models;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using MusicPlayer.Models.Interfaces;
using MusicPlayer.Models.Shuffle;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace MusicPlayer.Communication
{
    delegate void IsPlayingChangedEventHandler(ChangedEventArgs<bool> e);
    delegate void CurrentSongChangedEventHandler(CurrentSongChangedEventArgs e);

    static class ForegroundCommunicator
    {
        private static bool isUpdatingCurrentSong;
        private static ILibrary library;

        public static event IsPlayingChangedEventHandler IsPlayingChanged;
        public static event CurrentSongChangedEventHandler CurrentSongChanged;

        public static void Start(ILibrary srcLibrary)
        {
            library = srcLibrary;
            library.CurrentPlaylistChanged += Library_CurrentPlaylistChanged;
            Subscribe(library.CurrentPlaylist);

            BackgroundMediaPlayer.MessageReceivedFromBackground += OnMessageReceived;
        }

        public static void Stop()
        {
            library.CurrentPlaylistChanged -= Library_CurrentPlaylistChanged;
            Unsubscribe(library.CurrentPlaylist);

            BackgroundMediaPlayer.MessageReceivedFromForeground -= OnMessageReceived;
        }

        private static void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.LoopChanged += Playlist_LoopChanged;
            playlist.SongsChanged += Playlist_SongsChanged;

            Subscribe(playlist.Songs);
        }

        private static void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.LoopChanged -= Playlist_LoopChanged;
            playlist.SongsChanged -= Playlist_SongsChanged;

            Unsubscribe(playlist.Songs);
        }

        private static void Subscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.ShuffleChanged += Songs_ShuffleChanged;
            Subscribe(songs.Shuffle);
        }

        private static void Unsubscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.ShuffleChanged += Songs_ShuffleChanged;
            Unsubscribe(songs.Shuffle);
        }

        private static void Subscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed += Shuffle_Changed;
        }

        private static void Unsubscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed += Shuffle_Changed;
        }

        private static void Library_CurrentPlaylistChanged(object sender, ChangedEventArgs<IPlaylist> e)
        {
            PlaylistMessage message = new PlaylistMessage()
            {
                PositionTicks = e.NewValue?.Position.Ticks ?? 0,
                CurrentSong = e.NewValue?.CurrentSong,
                Loop = e.NewValue?.Loop ?? LoopType.Off,
                Songs = e.NewValue?.Songs.Shuffle.ToArray() ?? new Song[0],
            };
            Send(ForegroundMessageType.SetPlaylist, XmlConverter.Serialize(message));
        }

        private static void Playlist_LoopChanged(object sender, ChangedEventArgs<LoopType> e)
        {
            Send(ForegroundMessageType.SetLoop, e.NewValue.ToString());
        }

        private static void Playlist_SongsChanged(object sender, SongsChangedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)sender;
            string value = XmlConverter.Serialize(playlist.Songs.Shuffle.ToArray());
            Send(ForegroundMessageType.SetSongs, value);
        }

        private static void Songs_ShuffleChanged(object sender, ShuffleChangedEventArgs e)
        {
            ISongCollection songs = (ISongCollection)sender;
            string value = XmlConverter.Serialize(songs.Shuffle.ToArray());
            Send(ForegroundMessageType.SetSongs, value);
        }

        private static void Shuffle_Changed(object sender, ShuffleCollectionChangedEventArgs e)
        {
            IShuffleCollection shuffle = (IShuffleCollection)sender;
            string value = XmlConverter.Serialize(shuffle.ToArray());
            Send(ForegroundMessageType.SetSongs, value);
        }

        public static void SeekPosition(TimeSpan position)
        {
            Send(ForegroundMessageType.SetPosition, position.Ticks.ToString());
        }

        public static void SetSong(Song? song, TimeSpan position)
        {
            if (isUpdatingCurrentSong) return;

            string value = XmlConverter.Serialize(new CurrentSongMessage(song, position));
            Send(ForegroundMessageType.SetCurrentSong, value);
        }

        public static void Play()
        {
            Send(ForegroundMessageType.Play);
        }

        public static void Pause()
        {
            Send(ForegroundMessageType.Pause);
        }

        public static void Next()
        {
            Send(ForegroundMessageType.Next);
        }

        public static void Previous()
        {
            Send(ForegroundMessageType.Previous);
        }

        private static void Send(ForegroundMessageType type, string value = "")
        {
            ValueSet vs = new ValueSet()
            {
                {Constants.TypeKey, type.ToString()},
                {Constants.ValueKey, value}
            };
            try
            {
                BackgroundMediaPlayer.SendMessageToBackground(vs);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("Fore send error", e, type, value);
            }
        }

        private static async void OnMessageReceived(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            BackgroundMessageType type = GetType(e.Data);
            string value = e.Data[Constants.ValueKey].ToString();
            MobileDebug.Service.WriteEvent("ForeCom_Receive", type, value);
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.
                    RunAsync(CoreDispatcherPriority.Normal, () => HandleReceivedMessage(type, value));
            }
            catch (Exception exc)
            {
                MobileDebug.Service.WriteEvent("ForeComReceiveError", exc, type, value);
            }
        }

        private static void HandleReceivedMessage(BackgroundMessageType type, string value)
        {
            switch (type)
            {
                case BackgroundMessageType.SetCurrentSong:
                    isUpdatingCurrentSong = true;

                    Song newCurrentSong;
                    CurrentSongChangedEventArgs args;
                    IPlaylist currentPlaylist = library.CurrentPlaylist;

                    if (currentPlaylist != null &&
                        currentPlaylist.Songs.TryFirst(s => s.FullPath == value, out newCurrentSong))
                    {
                        args = new CurrentSongChangedEventArgs(newCurrentSong);
                    }
                    else args = new CurrentSongChangedEventArgs(null);

                    CurrentSongChanged?.Invoke(args);
                    isUpdatingCurrentSong = false;
                    break;

                case BackgroundMessageType.SetIsPlaying:
                    bool isPlaying = bool.Parse(value);
                    IsPlayingChanged?.Invoke(new ChangedEventArgs<bool>(!isPlaying, isPlaying));
                    break;
            }
        }

        private static BackgroundMessageType GetType(ValueSet vs)
        {
            return Utils.ParseEnum<BackgroundMessageType>(vs[Constants.TypeKey].ToString());
        }
    }
}
