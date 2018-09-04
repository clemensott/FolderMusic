﻿using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;

namespace MusicPlayer
{
    public static class Extention
    {
        public static IShuffleCollection GetShuffleOffCollection(this IPlaylist playlist)
        {
            return new ShuffleOffCollection(playlist, playlist.Songs);
        }

        public static TimeSpan GetCurrentSongPosition(this IPlaylist playlist)
        {
            double percent = playlist?.CurrentSongPositionPercent ?? 0;
            double duration = playlist?.CurrentSong?.DurationMilliseconds ?? Song.DefaultDuration;

            return TimeSpan.FromMilliseconds(percent * duration);
        }
    }
}
