using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using MusicPlayer.Models;
using MusicPlayer.Models.Enums;
using MusicPlayer.Models.Foreground.Interfaces;
using MusicPlayer.Models.Foreground.Shuffle;

namespace MusicPlayer
{
    public static class Utils
    {
        public static IShuffleCollection GetShuffleOffCollection(this ISongCollection songs)
        {
            return new ShuffleOffCollection(songs);
        }

        public static TimeSpan Multiply(this TimeSpan ts, double factor)
        {
            return TimeSpan.FromDays(ts.TotalDays * factor);
        }

        public static void SetNextLoop(this IPlaylist playlist)
        {
            switch (playlist.Loop)
            {
                case LoopType.All:
                    playlist.Loop = LoopType.Current;
                    break;

                case LoopType.Current:
                    playlist.Loop = LoopType.Off;
                    break;

                case LoopType.Off:
                    playlist.Loop = LoopType.All;
                    break;
            }
        }

        public static void SetNextShuffle(this ISongCollection songs, Song? currentSong)
        {
            switch (songs.Shuffle.Type)
            {
                case ShuffleType.Off:
                    songs.SetShuffleType(ShuffleType.Path, currentSong);
                    break;

                case ShuffleType.Path:
                    songs.SetShuffleType(ShuffleType.OneTime, currentSong);
                    break;

                case ShuffleType.OneTime:
                    songs.SetShuffleType(ShuffleType.Off, currentSong);
                    break;
            }
        }

        public static IEnumerable<T> RepeatOnce<T>(T item)
        {
            yield return item;
        }

        public static int IndexOf<T>(this IEnumerable<T> items, T searchItem)
        {
            IList<T> list = items as IList<T>;
            if (list != null) return list.IndexOf(searchItem);

            int i = 0;

            foreach (T item in items)
            {
                if (item.Equals(searchItem)) return i;

                i++;
            }

            return -1;
        }

        public static string XmlSerialize(Song? song)
        {
            return song.HasValue ? XmlConverter.Serialize(song) : string.Empty;
        }

        public static Song? XmlDeserializeSong(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;

            return XmlConverter.Deserialize<Song>(xml);
        }

        public static string Serialize(TimeSpan ts)
        {
            return ts.Ticks.ToString();
        }

        public static TimeSpan DeserializeTimeSpan(string text)
        {
            return TimeSpan.FromTicks(long.Parse(text));
        }

        public static T ParseEnum<T>(string text)
        {
            return (T)Enum.Parse(typeof(T), text);
        }

        public static bool BothNullOrSequenceEqual<T>(this IEnumerable<T> enum1, IEnumerable<T> enum2)
        {
            if (ReferenceEquals(enum1, enum2)) return true;
            if (enum1 == null || enum2 == null) return false;

            return enum1.SequenceEqual(enum2);
        }

        public static bool TryFirst<TSource>(this IEnumerable<TSource> src, Func<TSource, bool> predicate,
            out TSource first)
        {
            foreach (TSource item in src)
            {
                if (!predicate(item)) continue;

                first = item;
                return true;
            }

            first = default(TSource);
            return false;
        }

        public static bool TryGetSong(this IEnumerable<Song> src, string path, out Song song)
        {
            return TryFirst(src, s => s.FullPath == path, out song);
        }
    }
}
