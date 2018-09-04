using FolderMusicLib;
using LibraryLib;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Media.Playback;

namespace BackgroundTask
{
    public sealed class ForegroundCommunicator
    {
        private static Song CurrentSong { get { return Library.Current.CurrentPlaylist.CurrentSong; } }

        public static void SendPause()
        {
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { "Pause", "" } });
        }

        public static void SendSongsIndexAndShuffleIfComplete()
        {
            ValueSet valueSet = new ValueSet();

            if (Library.Current.CurrentPlaylist.Shuffle == ShuffleKind.Complete)
            {
                valueSet.Add("SongsIndexAndShuffle", Library.Current.CurrentPlaylist.SongsIndex.ToString());
                valueSet.Add("ShuffleKind", XmlConverter.Serialize(Library.Current.CurrentPlaylist.Shuffle));
                valueSet.Add("ShuffleList", XmlConverter.Serialize(Library.Current.CurrentPlaylist.ShuffleList));
            }
            else valueSet.Add("SongsIndex", Library.Current.CurrentPlaylist.SongsIndex.ToString());

            BackgroundMediaPlayer.SendMessageToForeground(valueSet);
        }

        public static void SendXmlText()
        {
            if (Library.IsLoaded)
            {
                BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { "XmlText", Library.Current.GetXmlText() } });
            }
        }

        public static void SendSkip()
        {
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { "Skip", "" } });
        }

        public static void MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            ValueSet valueSet = e.Data;

            foreach (string key in valueSet.Keys)
            {
                switch (key)
                {
                    case "PlaylistsAndSongsIndex":
                        GetPlaylistsAndSongsIndex(valueSet);
                        return;

                    case "PlaylistsAndSongsIndexAndShuffle":
                        GetPlaylistsAndSongsIndexAndShuffle(valueSet);
                        return;

                    case "Play":
                        BackgroundAudioTask.Current.Play();
                        return;

                    case "Pause":
                        BackgroundAudioTask.Current.Pause();
                        return;

                    case "Loop":
                        GetLoop(valueSet);
                        return;

                    case "Shuffle":
                        GetShuffle(valueSet);
                        return;

                    case "CurrentPlaylistIndex":
                        GetCurrentPlaylistIndex(valueSet);
                        return;

                    case "GetXmlText":
                        SendXmlText();
                        return;

                    case "PlaylistXML":
                        GetPlaylistXML(valueSet);
                        return;

                    case "LoadXML":
                        GetLoadXML(valueSet);
                        return;

                    case "SongXML":
                        GetSongXML(valueSet);
                        return;

                    case "RemoveSong":
                        GetRemoveSong(valueSet);
                        return;

                    case "RemovePlaylist":
                        GetRemovePlaylist(valueSet);
                        return;
                }
            }
        }

        private static void GetPlaylistsAndSongsIndex(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string[] parts = valueSet["PlaylistsAndSongsIndex"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);

            if (playlistIndex == -1 || songsIndex == -1) return;

            Library.Current.CurrentPlaylistIndex = playlistIndex;
            Library.Current.CurrentPlaylist.SongsIndex = songsIndex;

            PlaySongIfOther(currentSongPath);
        }

        private static void GetPlaylistsAndSongsIndexAndShuffle(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string[] parts = valueSet["PlaylistsAndSongsIndexAndShuffle"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);

            if (playlistIndex == -1 || songsIndex == -1) return;

            Library.Current.CurrentPlaylistIndex = playlistIndex;
            Library.Current.CurrentPlaylist.SongsIndex = songsIndex;
            Library.Current.CurrentPlaylist.Shuffle = XmlConverter.Deserialize<ShuffleKind>(valueSet["ShuffleKind"].ToString());
            Library.Current.CurrentPlaylist.ShuffleList = XmlConverter.Deserialize<List<int>>(valueSet["ShuffleList"].ToString());

            PlaySongIfOther(currentSongPath);
        }

        private async static void GetLoop(ValueSet valueSet)
        {
            int playlistIndex = int.Parse(valueSet["Loop"].ToString());

            if (playlistIndex == -1) return;

            Library.Current[playlistIndex].Loop = XmlConverter.Deserialize<LoopKind>(valueSet["Kind"].ToString());
            BackgroundAudioTask.Current.SetLoopToBackgroundPlayer();

            await Library.Current.SaveAsync();
        }

        private async static void GetShuffle(ValueSet valueSet)
        {
            int playlistIndex = int.Parse(valueSet["Shuffle"].ToString());

            if (playlistIndex == -1) return;

            Library.Current[playlistIndex].Shuffle = XmlConverter.Deserialize<ShuffleKind>(valueSet["Kind"].ToString());
            Library.Current[playlistIndex].ShuffleList = XmlConverter.Deserialize<List<int>>(valueSet["List"].ToString());

            await Library.Current.SaveAsync();
        }

        private async static void GetCurrentPlaylistIndex(ValueSet valueSet)
        {
            Library.Current.CurrentPlaylistIndex = int.Parse(valueSet["CurrentPlaylistIndex"].ToString());
            BackgroundAudioTask.Current.SetCurrentSong(false);

            await Library.Current.SaveAsync();
        }

        private async static void GetPlaylistXML(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            int playlistIndex = int.Parse(valueSet["PlaylistXML"].ToString());

            if (playlistIndex == -1) return;

            Library.Current[playlistIndex] = XmlConverter.Deserialize<Playlist>(valueSet["XML"].ToString());
            PlaySongIfOther(currentSongPath);

            await Library.Current.SaveAsync();
        }

        private async static void GetLoadXML(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            Library.Current.Load(valueSet["LoadXML"].ToString());
            PlaySongIfOther(currentSongPath);

            await Library.Current.SaveAsync();
        }

        private static async void GetSongXML(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string[] parts = valueSet["SongXML"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);

            if (playlistIndex == -1 || songsIndex == -1) return;

            Library.Current[playlistIndex][songsIndex] = XmlConverter.Deserialize<Song>(valueSet["XML"].ToString());
            PlaySongIfOther(currentSongPath);

            await Library.Current.SaveAsync();
        }

        private static async void GetRemoveSong(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string[] parts = valueSet["RemoveSong"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);

            if (playlistIndex == -1 || songsIndex == -1) return;

            Library.Current.RemoveSongFromPlaylist(Library.Current[playlistIndex], songsIndex);
            PlaySongIfOther(currentSongPath);

            await Library.Current.SaveAsync();
        }

        private static async void GetRemovePlaylist(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            Library.Current.DeleteAt(int.Parse(valueSet["RemovePlaylist"].ToString()));
            PlaySongIfOther(currentSongPath);

            await Library.Current.SaveAsync();
        }

        private static void PlaySongIfOther(string path)
        {
            if (path != CurrentSong.Path) BackgroundAudioTask.Current.SetCurrentSong(BackgroundAudioTask.Current.IsPlaying);
        }
    }
}
