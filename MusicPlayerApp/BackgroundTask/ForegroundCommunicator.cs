using FolderMusicLib;
using LibraryLib;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Playback;

namespace BackgroundTask
{
    public sealed class ForegroundCommunicator
    {
        private static Song CurrentSong { get { return Library.Current.CurrentPlaylist.CurrentSong; } }

        public static void SendPause()
        {
            Send(new ValueSet { { "Pause", "" } });
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

            valueSet.Add("Position", BackgroundMediaPlayer.Current.Position.TotalMilliseconds.ToString());
            valueSet.Add("NaturalDuration", Library.Current.CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds.ToString());

            Send(valueSet);
        }

        public static void SendXmlText()
        {
            string text;

            if (!Library.IsLoaded) text = "NotLoaded";
            else
            {
                if (Library.Current.IsEmpty) text = "LoadedButEmpty";
                else text = Library.Current.GetXmlText();
            }

            Send(new ValueSet { { "XmlText", text } });
        }

        public static void SendSkip()
        {
            Send(new ValueSet { { "Skip", "" } });
        }

        private static void Send(ValueSet valueSet)
        {
            BackgroundMediaPlayer.SendMessageToForeground(valueSet);
        }

        public static void SetReceivedEvent()
        {
            BackgroundMediaPlayer.MessageReceivedFromForeground += MessageReceivedFromForeground;
        }

        private static void MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            try
            {
                ValueSet valueSet = e.Data;

                foreach (string key in valueSet.Keys) if (MessageReceivedSwitchCase(key, valueSet)) return;
            }
            catch { }
        }

        private static bool MessageReceivedSwitchCase(string key, ValueSet valueSet)
        {
            switch (key)
            {
                case "PlaylistsAndSongsIndex":
                    GetPlaylistsAndSongsIndex(valueSet);
                    return true;

                case "PlaylistsAndSongsIndexAndShuffle":
                    GetPlaylistsAndSongsIndexAndShuffle(valueSet);
                    return true;

                case "Play":
                    BackgroundAudioTask.Current.Play();
                    return true;

                case "Pause":
                    BackgroundAudioTask.Current.Pause();
                    return true;

                case "Loop":
                    GetLoop(valueSet);
                    return true;

                case "Shuffle":
                    GetShuffle(valueSet);
                    return true;

                case "CurrentPlaylistIndex":
                    GetCurrentPlaylistIndex(valueSet);
                    return true;

                case "GetXmlText":
                    SendXmlText();
                    return true;

                case "PlaylistXML":
                    GetPlaylistXML(valueSet);
                    return true;

                case "LoadXML":
                    GetLoadXML(valueSet);
                    return true;

                case "SongXML":
                    GetSongXML(valueSet);
                    return true;

                case "RemoveSong":
                    GetRemoveSong(valueSet);
                    return true;

                case "RemovePlaylist":
                    GetRemovePlaylist(valueSet);
                    return true;
            }

            return false;
        }

        private static void GetPlaylistsAndSongsIndex(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string[] parts = valueSet["PlaylistsAndSongsIndex"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);

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

            Library.Current.CurrentPlaylistIndex = playlistIndex;
            Library.Current.CurrentPlaylist.SongsIndex = songsIndex;
            Library.Current.CurrentPlaylist.Shuffle = XmlConverter.Deserialize<ShuffleKind>(valueSet["ShuffleKind"].ToString());
            Library.Current.CurrentPlaylist.ShuffleList = XmlConverter.Deserialize<List<int>>(valueSet["ShuffleList"].ToString());

            PlaySongIfOther(currentSongPath);
        }

        private async static void GetLoop(ValueSet valueSet)
        {
            int playlistIndex = int.Parse(valueSet["Loop"].ToString());

            Library.Current[playlistIndex].Loop = XmlConverter.Deserialize<LoopKind>(valueSet["Kind"].ToString());
            BackgroundAudioTask.Current.SetLoopToBackgroundPlayer();

            await Library.Current.SaveAsync();
        }

        private async static void GetShuffle(ValueSet valueSet)
        {
            int playlistIndex = int.Parse(valueSet["Shuffle"].ToString());

            Library.Current[playlistIndex].Shuffle = XmlConverter.Deserialize<ShuffleKind>(valueSet["Kind"].ToString());
            Library.Current[playlistIndex].ShuffleList = XmlConverter.Deserialize<List<int>>(valueSet["List"].ToString());

            await Library.Current.SaveAsync();
        }

        private async static void GetCurrentPlaylistIndex(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            Library.Current.CurrentPlaylistIndex = int.Parse(valueSet["CurrentPlaylistIndex"].ToString());

            PlaySongIfOther(currentSongPath);

            await Library.Current.SaveAsync();
        }

        private async static void GetPlaylistXML(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            int playlistIndex = int.Parse(valueSet["PlaylistXML"].ToString());

            Library.Current[playlistIndex] = XmlConverter.Deserialize<Playlist>(valueSet["XML"].ToString());
            PlaySongIfOther(currentSongPath);

            await Library.Current.SaveAsync();
        }

        private async static void GetLoadXML(ValueSet valueSet)
        {
            Library.Current.Load(valueSet["LoadXML"].ToString());
            BackgroundAudioTask.Current.SetCurrentSong();

            await Library.Current.SaveAsync();
        }

        private static async void GetSongXML(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string[] parts = valueSet["SongXML"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);

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
