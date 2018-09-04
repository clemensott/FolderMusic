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
            Send("Pause", "");
        }

        public static void SendSongsIndexAndShuffleListIfIsShuffleComplete()
        {
            ValueSet valueSet = new ValueSet();

            if (Library.Current.CurrentPlaylist.Shuffle == ShuffleKind.Complete)
            {
                valueSet.Add("SongsIndexAndShuffle", Library.Current.CurrentPlaylist.SongsIndex.ToString());
                valueSet.Add("ShuffleKind", XmlConverter.Serialize(Library.Current.CurrentPlaylist.Shuffle));
                valueSet.Add("ShuffleList", XmlConverter.Serialize(Library.Current.CurrentPlaylist.ShuffleList));
            }
            else valueSet.Add("SongsIndex", Library.Current.CurrentPlaylist.SongsIndex.ToString());

            valueSet.Add("PositionMillis", BackgroundMediaPlayer.Current.Position.TotalMilliseconds.ToString());
            valueSet.Add("NaturalDurationMillis", Library.Current.CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds.ToString());

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

            BackgroundAudioTask.Current.ActivateSystemMediaTransportControl();

            Send("XmlText", text);
        }

        public static void SendSkip()
        {
            Send("Skip", "");
        }

        private static void Send(string key, string value)
        {
            Send(new ValueSet { { key, value } });
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
                    break;

                case "PlaylistsAndSongsIndexAndShuffle":
                    GetPlaylistsAndSongsIndexAndShuffle(valueSet);
                    break;

                case "CurrentPlaylistIndex":
                    SetCurrentPlaylistIndex(valueSet);
                    break;

                case "Play":
                    BackgroundAudioTask.Current.Play();
                    break;

                case "Pause":
                    BackgroundAudioTask.Current.Pause();
                    break;

                case "Loop":
                    GetLoop(valueSet);
                    break;

                case "Shuffle":
                    GetShuffle(valueSet);
                    break;

                case "GetXmlText":
                    SendXmlText();
                    break;

                case "LoadXML":
                    GetLoadXML(valueSet);
                    break;

                case "SongXML":
                    GetSongXML(valueSet);
                    break;

                case "PlaylistXML":
                    GetPlaylistXML(valueSet);
                    break;

                case "RemoveSong":
                    GetRemoveSong(valueSet);
                    break;

                case "RemovePlaylist":
                    GetRemovePlaylist(valueSet);
                    break;

                case "RingerChanged":
                    Ringer.Current.ReloadTimes();
                    break;

                case "AskForReply":
                    FolderMusicDebug.SaveTextClass.Current.SaveText("AskedForReply");
                    Send("Reply", "");
                    break;

                default:
                    return false;
            }

            FolderMusicDebug.SaveTextClass.Current.SaveText("BackReceive", key, valueSet[key]);

            return true;
        }

        private static void GetPlaylistsAndSongsIndex(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string path = valueSet["Path"].ToString();
            string[] parts = valueSet["PlaylistsAndSongsIndex"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);

            if (Library.Current[playlistIndex][songsIndex].Path != path &&
                !Library.Current.HavePlaylistIndexAndSongsIndex(path, out playlistIndex, out songsIndex)) return;

            Library.Current.CurrentPlaylistIndex = playlistIndex;
            Library.Current.CurrentPlaylist.SongsIndex = songsIndex;

            PlaySongIfOther(currentSongPath);
        }

        private static void GetPlaylistsAndSongsIndexAndShuffle(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string path = valueSet["Path"].ToString();
            string[] parts = valueSet["PlaylistsAndSongsIndexAndShuffle"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);

            if (Library.Current[playlistIndex][songsIndex].Path != path &&
                !Library.Current.HavePlaylistIndexAndSongsIndex(path, out playlistIndex, out songsIndex)) return;

            Library.Current.CurrentPlaylistIndex = playlistIndex;
            Library.Current.CurrentPlaylist.SongsIndex = songsIndex;
            Library.Current.CurrentPlaylist.Shuffle = XmlConverter.Deserialize<ShuffleKind>(valueSet["ShuffleKind"].ToString());
            Library.Current.CurrentPlaylist.ShuffleList = XmlConverter.Deserialize<List<int>>(valueSet["ShuffleList"].ToString());

            PlaySongIfOther(currentSongPath);
        }

        private static void SetCurrentPlaylistIndex(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            int playlistIndex = int.Parse(valueSet["CurrentPlaylistIndex"].ToString());
            string path = valueSet["Path"].ToString();

            if (Library.Current[playlistIndex].AbsolutePath != path &&
                !Library.Current.HavePlaylistIndex(path, out playlistIndex)) return;

            Library.Current.CurrentPlaylistIndex = playlistIndex;

            PlaySongIfOther(currentSongPath);

            Library.Current.SaveAsync();
        }

        private static void GetLoop(ValueSet valueSet)
        {
            int playlistIndex = int.Parse(valueSet["Loop"].ToString());

            Library.Current[playlistIndex].Loop = XmlConverter.Deserialize<LoopKind>(valueSet["Kind"].ToString());
            BackgroundAudioTask.Current.SetLoopToBackgroundPlayer();

            Library.Current.SaveAsync();
        }

        private static void GetShuffle(ValueSet valueSet)
        {
            int playlistIndex = int.Parse(valueSet["Shuffle"].ToString());

            Library.Current[playlistIndex].Shuffle = XmlConverter.Deserialize<ShuffleKind>(valueSet["Kind"].ToString());
            Library.Current[playlistIndex].ShuffleList = XmlConverter.Deserialize<List<int>>(valueSet["List"].ToString());

            Library.Current.SaveAsync();
        }

        private static void GetLoadXML(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            Library.Current.Load(valueSet["LoadXML"].ToString());

            PlaySongIfOther(currentSongPath);

            Library.Current.SaveAsync();
        }

        private static void GetSongXML(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string[] parts = valueSet["SongXML"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);
            Song song = XmlConverter.Deserialize<Song>(valueSet["XML"].ToString());

            if (Library.Current[playlistIndex][songsIndex].Path != song.Path &&
                !Library.Current.HavePlaylistIndexAndSongsIndex(song.Path, out playlistIndex, out songsIndex)) return;

            Library.Current[playlistIndex][songsIndex] = song;
            PlaySongIfOther(currentSongPath);

            Library.Current.SaveAsync();
        }

        private static void GetPlaylistXML(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            int playlistIndex = int.Parse(valueSet["PlaylistXML"].ToString());
            Playlist playlist = XmlConverter.Deserialize<Playlist>(valueSet["XML"].ToString());

            if (Library.Current[playlistIndex].AbsolutePath != playlist.AbsolutePath &&
                !Library.Current.HavePlaylistIndex(playlist.AbsolutePath, out playlistIndex)) return;

            Library.Current[playlistIndex] = playlist;
            PlaySongIfOther(currentSongPath);

            Library.Current.SaveAsync();
        }

        private static void GetRemoveSong(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            string path = valueSet["Path"].ToString();
            string[] parts = valueSet["RemoveSong"].ToString().Split(';');
            int playlistIndex = int.Parse(parts[0]);
            int songsIndex = int.Parse(parts[1]);

            if (Library.Current[playlistIndex][songsIndex].Path != path &&
                !Library.Current.HavePlaylistIndexAndSongsIndex(path, out playlistIndex, out songsIndex)) return;

            Library.Current[playlistIndex].RemoveSong(songsIndex);
            PlaySongIfOther(currentSongPath);

            Library.Current.SaveAsync();
        }

        private static void GetRemovePlaylist(ValueSet valueSet)
        {
            string currentSongPath = CurrentSong.Path;

            int playlistIndex = int.Parse(valueSet["RemovePlaylist"].ToString());
            string path = valueSet["Path"].ToString();

            if (Library.Current[playlistIndex].AbsolutePath != path &&
                !Library.Current.HavePlaylistIndex(path, out playlistIndex)) return;

            Library.Current.DeleteAt(playlistIndex);
            PlaySongIfOther(currentSongPath);

            Library.Current.SaveAsync();
        }

        private static void PlaySongIfOther(string path)
        {
            if (path != CurrentSong.Path) BackgroundAudioTask.Current.SetCurrentSong(BackgroundAudioTask.Current.IsPlaying);
        }
    }
}
