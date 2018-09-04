using Windows.Foundation.Collections;
using Windows.Media.Playback;
using System.Collections.Generic;
using LibraryLib;
using System;

namespace FolderMusicLib
{
    public class BackgroundCommunicator
    {
        public static void SendPlaylistsAndSongsIndexAndShuffleIfComplete(Playlist playlist)
        {
            if (playlist.PlaylistIndex == -1 || playlist.SongsIndex == -1) return;

            ValueSet valueSet = new ValueSet();
            valueSet.Add("Path", playlist.CurrentSong.Path);

            if (playlist.Shuffle == ShuffleKind.Complete)
            {
                string playlistIndexAndSongsIndex = string.Format("{0};{1}", playlist.PlaylistIndex, playlist.SongsIndex);
                valueSet.Add("PlaylistsAndSongsIndexAndShuffle", playlistIndexAndSongsIndex);
                valueSet.Add("ShuffleKind", XmlConverter.Serialize(playlist.Shuffle));
                valueSet.Add("ShuffleList", XmlConverter.Serialize(playlist.ShuffleList));
            }
            else valueSet.Add("PlaylistsAndSongsIndex", string.Format("{0};{1}", playlist.PlaylistIndex, playlist.SongsIndex));

            Send(valueSet);
        }

        public static void SendCurrentPlaylistIndex()
        {
            if (Library.Current.CurrentPlaylistIndex == -1) return;

            ValueSet valueSet = new ValueSet();
            valueSet.Add("CurrentPlaylistIndex", Library.Current.CurrentPlaylistIndex.ToString());
            valueSet.Add("Path", Library.Current.CurrentPlaylist.AbsolutePath);

            Send(valueSet);
        }

        public static void SendPlay()
        {
            Send(new ValueSet { { "Play", "" } });
        }

        public static void SendPause()
        {
            Send(new ValueSet { { "Pause", "" } });
        }

        public static void SendLoop(Playlist playlist)
        {
            if (playlist.PlaylistIndex == -1) return;

            string loopXML = XmlConverter.Serialize(playlist.Loop);
            ValueSet valueSet = new ValueSet();

            valueSet.Add("Loop", playlist.PlaylistIndex.ToString());
            valueSet.Add("Kind", loopXML);

            Send(valueSet);
        }

        public static void SendShuffle(Playlist playlist)
        {
            if (playlist.PlaylistIndex == -1) return;

            string shuffleXML = XmlConverter.Serialize(playlist.Shuffle);
            string shuffleListXML = XmlConverter.Serialize(playlist.ShuffleList);
            ValueSet valueSet = new ValueSet();

            valueSet.Add("Shuffle", playlist.PlaylistIndex.ToString());
            valueSet.Add("Kind", shuffleXML);
            valueSet.Add("List", shuffleListXML);

            Send(valueSet);
        }

        public static void SendGetXmlText()
        {
            Send(new ValueSet { { "GetXmlText", "" } });
        }

        public static void SendLoadXML()
        {
            Send(new ValueSet { { "LoadXML", Library.Current.GetXmlText() } });
        }

        public static void SendSongXML(Song song)
        {
            int playlistIndex, songsIndex;

            if (!Library.Current.HavePlaylistIndexAndSongsIndex(song, out playlistIndex, out songsIndex)) return;
            
            ValueSet valueSet = new ValueSet();
            valueSet.Add("SongXML", string.Format("{0};{1}", playlistIndex, songsIndex));
            valueSet.Add("XML", XmlConverter.Serialize(song));

            Send(valueSet);
        }

        public static void SendPlaylistXML(Playlist playlist)
        {
            if (playlist.PlaylistIndex == -1) return;

            int playlistIndex = playlist.PlaylistIndex;
            ValueSet valueSet = new ValueSet();
            valueSet.Add("PlaylistXML", playlistIndex.ToString());
            valueSet.Add("XML", XmlConverter.Serialize(Library.Current[playlistIndex]));

            Send(valueSet);
        }

        public static void SendRemoveSong(int playlistIndex, int songsIndex)
        {
            if (playlistIndex == -1 || songsIndex == -1) return;

            ValueSet valueSet = new ValueSet();
            valueSet.Add("RemoveSong", string.Format("{0};{1}", playlistIndex, songsIndex));
            valueSet.Add("Path", Library.Current[playlistIndex][songsIndex].Path);

            Send(valueSet);
        }

        public static void SendRemovePlaylist(Playlist playlist)
        {
            if (playlist.PlaylistIndex == -1) return;

            ValueSet valueSet = new ValueSet();
            valueSet.Add("RemovePlaylist", playlist.PlaylistIndex.ToString());
            valueSet.Add("Path", playlist.AbsolutePath);

            Send(valueSet);
        }

        private static void Send(ValueSet valueSet)
        {
            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SetReceivedEvent()
        {
            BackgroundMediaPlayer.MessageReceivedFromBackground += MessageReceivedFromBackground;
        }

        private static void MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
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
                case "SongsIndex":
                    GetSongsIndex(valueSet);
                    return true;

                case "SongsIndexAndShuffle":
                    GetSongsIndexAndShuffle(valueSet);
                    return true;

                case "XmlText":
                    GetXmlText(valueSet);
                    return true;

                case "Pause":
                    ViewModel.Current.UpdatePlayPauseIconAndText();
                    return true;

                case "Skip":
                    SkipSongsPage.NavigateToIfSkipSongsExists();
                    return true;
            }

            return false;
        }

        private static void GetSongsIndex(ValueSet valueSet)
        {
            int songsIndex = int.Parse(valueSet["SongsIndex"].ToString());
            double position = double.Parse(valueSet["Position"].ToString());
            double naturalDuration = double.Parse(valueSet["NaturalDuration"].ToString());

            ViewModel.Current.CurrentPlaylist.SongsIndex = songsIndex;
            ViewModel.Current.SliderValue = position;
            ViewModel.Current.SliderMaximum = naturalDuration;
        }

        private static void GetSongsIndexAndShuffle(ValueSet valueSet)
        {
            int songsIndex = int.Parse(valueSet["SongsIndexAndShuffle"].ToString());
            double naturalDuration = double.Parse(valueSet["NaturalDuration"].ToString());
            ShuffleKind shuffle = XmlConverter.Deserialize<ShuffleKind>(valueSet["ShuffleKind"].ToString());
            List<int> shuffleList = XmlConverter.Deserialize<List<int>>(valueSet["ShuffleList"].ToString());

            ViewModel.Current.CurrentPlaylist.Shuffle = shuffle;

            ViewModel.Current.CurrentPlaylist.ShuffleList = shuffleList;
            ViewModel.Current.CurrentPlaylist.UpdateSongsAndShuffleListSongs();

            ViewModel.Current.CurrentPlaylist.SongsIndex = songsIndex;
            ViewModel.Current.CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds = naturalDuration;
        }

        private async static void GetXmlText(ValueSet valueSet)
        {
            string text = valueSet["XmlText"].ToString();

            if (text == "NotLoaded") return;
            else if (text == "LoadedButEmpty")
            {
                CurrentSong.Current.Unset();
                Library.Current.SetLoaded();
                await SkipSongs.Delete();
            }
            else
            {
                Library.Current.Load(text);
                await SkipSongsPage.NavigateToIfSkipSongsExists();
            }
        }
    }
}
