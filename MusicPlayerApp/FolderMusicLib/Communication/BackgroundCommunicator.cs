using Windows.Foundation.Collections;
using Windows.Media.Playback;
using MusicPlayer.Communication.Messages;
using MusicPlayer.Models;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.EventArgs;
using System;

namespace MusicPlayer.Communication
{
    class BackgroundCommunicator
    {
        private bool isRunning;

        public event EventHandler<CurrentSongReceivedEventArgs> CurrentSongReceived;
        public event EventHandler<TimeSpan> PositionReceived;
        public event EventHandler<PlaylistReceivedEventArgs> PlaylistReceived;
        public event EventHandler<Song[]> SongsReceived;
        public event EventHandler<double> PlaybackRateReceived;
        public event EventHandler<LoopType> LoopReceived;
        public event EventHandler PlayReceived;
        public event EventHandler PauseReceived;
        public event EventHandler NextReceived;
        public event EventHandler PreviousReceived;

        public void Start()
        {
            BackgroundMediaPlayer.MessageReceivedFromForeground += OnMessageReceived;
            isRunning = true;

            Send(BackgroundMessageType.Ping);

            MobileDebug.Service.WriteEvent("BackComStarted");
        }

        public void Stop()
        {
            BackgroundMediaPlayer.MessageReceivedFromForeground -= OnMessageReceived;
            isRunning = false;
        }

        public void SendIsPlaying(bool isPlaying)
        {
            Send(BackgroundMessageType.SetIsPlaying, isPlaying.ToString());
        }

        public void SendCurrentSong(Song? song)
        {
            Send(BackgroundMessageType.SetCurrentSong, song?.FullPath ?? string.Empty);
        }

        private void Send(BackgroundMessageType type, string value = "")
        {
            if (!isRunning)
            {
                MobileDebug.Service.WriteEvent("BackComDontSend", type);
                return;
            }

            ValueSet vs = new ValueSet()
            {
                {Constants.TypeKey, type.ToString()},
                {Constants.ValueKey, value}
            };

            BackgroundMediaPlayer.SendMessageToForeground(vs);
        }

        private void OnMessageReceived(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            string value = e.Data[Constants.ValueKey].ToString();
            MobileDebug.Service.WriteEvent("BackCom_Receive", GetType(e.Data), value.Length);
            switch (GetType(e.Data))
            {
                case ForegroundMessageType.SetCurrentSong:
                    CurrentSongMessage currentSongMessage = XmlConverter.Deserialize<CurrentSongMessage>(value);
                    CurrentSongReceived?.Invoke(this, new CurrentSongReceivedEventArgs(
                        currentSongMessage.Song,
                        TimeSpan.FromTicks(currentSongMessage.PositionTicks)
                    ));
                    break;

                case ForegroundMessageType.SetPosition:
                    PositionReceived?.Invoke(this, TimeSpan.FromTicks(long.Parse(value)));
                    break;

                case ForegroundMessageType.SetPlaylist:
                    PlaylistMessage playlistMessage = XmlConverter.Deserialize<PlaylistMessage>(value);
                    PlaylistReceived?.Invoke(this, new PlaylistReceivedEventArgs(
                        playlistMessage.CurrentSong,
                        TimeSpan.FromTicks(playlistMessage.PositionTicks),
                        playlistMessage.PlaybackRate,
                        playlistMessage.Loop,
                        playlistMessage.Songs
                    ));
                    break;

                case ForegroundMessageType.SetSongs:
                    SongsReceived?.Invoke(this, XmlConverter.Deserialize<Song[]>(value));
                    break;

                case ForegroundMessageType.SetPlaybackRate:
                    PlaybackRateReceived?.Invoke(this, double.Parse(value));
                    break;

                case ForegroundMessageType.SetLoop:
                    LoopReceived?.Invoke(this, Utils.ParseEnum<LoopType>(value));
                    break;

                case ForegroundMessageType.Play:
                    PlayReceived?.Invoke(this, EventArgs.Empty);
                    break;

                case ForegroundMessageType.Pause:
                    PauseReceived?.Invoke(this, EventArgs.Empty);
                    break;

                case ForegroundMessageType.Next:
                    NextReceived?.Invoke(this, EventArgs.Empty);
                    break;

                case ForegroundMessageType.Previous:
                    PreviousReceived?.Invoke(this, EventArgs.Empty);
                    break;

                case ForegroundMessageType.Ping:
                    Send(BackgroundMessageType.Ping);
                    break;
            }
        }

        private static ForegroundMessageType GetType(ValueSet vs)
        {
            return Utils.ParseEnum<ForegroundMessageType>(vs[Constants.TypeKey].ToString());
        }
    }
}
