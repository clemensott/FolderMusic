using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer
{
    public static class Utils
    {
        public static IShuffleCollection GetShuffleOffCollection(this ISongCollection songs)
        {
            return new ShuffleOffCollection(songs);
        }

        public static TimeSpan GetCurrentSongPosition(this IPlaylist playlist)
        {
            double percent = playlist?.CurrentSongPosition ?? 0;
            double duration = playlist?.CurrentSong?.DurationMilliseconds ?? Song.DefaultDuration;

            return TimeSpan.FromMilliseconds(percent * duration);
        }

        public static void ChangeCurrentSong(this IPlaylist playlist, int offset)
        {
            int count = playlist.Songs.Shuffle.Count;

            if (count == 0) return;

            int shuffleSongsIndex = playlist.Songs.Shuffle.IndexOf(playlist.CurrentSong);
            shuffleSongsIndex = (shuffleSongsIndex + offset + count) % count;
            playlist.CurrentSong = playlist.Songs.Shuffle.ElementAt(shuffleSongsIndex);

            if (playlist.CurrentSong.Failed)
            {
                if (playlist.Songs.All(x => x.Failed)) return;

                ChangeCurrentSong(playlist, offset);
            }
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

        public static void SetNextShuffle(this ISongCollection songs)
        {
            MobileDebug.Service.WriteEvent("SetNextShuffle1", songs?.Shuffle.Type);
            switch (songs.Shuffle.Type)
            {
                case ShuffleType.Off:
                    songs.SetShuffleType(ShuffleType.OneTime);
                    break;

                case ShuffleType.OneTime:
                    songs.SetShuffleType(ShuffleType.Complete);
                    break;

                case ShuffleType.Complete:
                    MobileDebug.Service.WriteEvent("SetNextShuffle2", songs?.Shuffle.Type);
                    songs.SetShuffleType(ShuffleType.Off);
                    MobileDebug.Service.WriteEvent("SetNextShuffle3", songs?.Shuffle.Type);
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
    }
}
