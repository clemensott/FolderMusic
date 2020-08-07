using System.Linq;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using MusicPlayer.Communication.Messages;
using MusicPlayer.Handler;
using MusicPlayer.Models;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using System;

namespace MusicPlayer.Communication
{
    public static class BackgroundCommunicator
    {
        private static BackgroundPlayerHandler handler;

        public static void Start(BackgroundPlayerHandler srcHandler)
        {
            handler = srcHandler;
            handler.IsPlayingChanged += Handler_IsPlayingChanged;
            handler.CurrentSongChanged += Handler_CurrentSongChanged;

            BackgroundMediaPlayer.MessageReceivedFromForeground += OnMessageReceived;
        }

        public static void Stop()
        {
            handler.IsPlayingChanged -= Handler_IsPlayingChanged;
            handler.CurrentSongChanged -= Handler_CurrentSongChanged;

            BackgroundMediaPlayer.MessageReceivedFromForeground -= OnMessageReceived;
        }

        private static void Handler_IsPlayingChanged(object sender, ChangedEventArgs<bool> e)
        {
            Send(BackgroundMessageType.SetIsPlaying, e.NewValue.ToString());
        }

        private static void Handler_CurrentSongChanged(object sender, ChangedEventArgs<Song?> e)
        {
            Send(BackgroundMessageType.SetCurrentSong, e.NewValue?.FullPath ?? string.Empty);
        }

        private static void Send(BackgroundMessageType type, string value)
        {
            ValueSet vs = new ValueSet()
            {
                {Constants.TypeKey, type.ToString()},
                {Constants.ValueKey, value}
            };

            BackgroundMediaPlayer.SendMessageToForeground(vs);
        }

        private static async void OnMessageReceived(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            string value = e.Data[Constants.ValueKey].ToString();
            MobileDebug.Service.WriteEvent("BackCom_Receive", GetType(e.Data), value);
            switch (GetType(e.Data))
            {
                case ForegroundMessageType.SetCurrentSong:
                    CurrentSongMessage currentSongMessage = XmlConverter.Deserialize<CurrentSongMessage>(value);
                    await handler.SetSong(currentSongMessage.Song, currentSongMessage.Position);
                    break;

                case ForegroundMessageType.SetPosition:
                    TimeSpan position = TimeSpan.FromTicks(long.Parse(value));
                    handler.SeekToPosition(position);
                    break;

                case ForegroundMessageType.SetPlaylist:
                    PlaylistMessage playlistMessage = XmlConverter.Deserialize<PlaylistMessage>(value);
                    handler.Playlist.Loop = playlistMessage.Loop;
                    handler.Playlist.Songs = playlistMessage.Songs;
                    await handler.SetSong(playlistMessage.CurrentSong, TimeSpan.FromTicks(playlistMessage.PositionTicks));
                    break;

                case ForegroundMessageType.SetSongs:
                    handler.Playlist.Songs = XmlConverter.Deserialize<Song[]>(value);
                    break;

                case ForegroundMessageType.SetLoop:
                    handler.Playlist.Loop = Utils.ParseEnum<LoopType>(value);
                    break;

                case ForegroundMessageType.Play:
                    await handler.Play();
                    break;

                case ForegroundMessageType.Pause:
                    handler.Pause();
                    break;

                case ForegroundMessageType.Next:
                    await handler.Next(false);
                    break;

                case ForegroundMessageType.Previous:
                    await handler.Previous();
                    break;
            }
        }

        private static ForegroundMessageType GetType(ValueSet vs)
        {
            return Utils.ParseEnum<ForegroundMessageType>(vs[Constants.TypeKey].ToString());
        }
    }
}
