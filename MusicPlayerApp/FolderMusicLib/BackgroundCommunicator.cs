using Windows.Foundation.Collections;
using Windows.Media.Playback;
using System.Collections.Generic;
using LibraryLib;

namespace FolderMusicLib
{
    public class BackgroundCommunicator
    {
        public static void SendPlaylistsAndSongsIndexAndShuffleIfComplete(Playlist playlist)
        {
            ValueSet valueSet = new ValueSet();

            if (playlist.Shuffle == ShuffleKind.Complete)
            {
                string playlistIndexAndSongsIndex = string.Format("{0};{1}", playlist.PlaylistIndex, playlist.SongsIndex);
                valueSet.Add("PlaylistsAndSongsIndexAndShuffle", playlistIndexAndSongsIndex);
                valueSet.Add("ShuffleKind", XmlConverter.Serialize(playlist.Shuffle));
                valueSet.Add("ShuffleList", XmlConverter.Serialize(playlist.ShuffleList));
            }
            else valueSet.Add("PlaylistsAndSongsIndex", string.Format("{0};{1}", playlist.PlaylistIndex, playlist.SongsIndex));

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendCurrentPlaylistIndex()
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add("CurrentPlaylistIndex", Library.Current.CurrentPlaylistIndex.ToString());

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendPlay()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "Play", "" } });
        }

        public static void SendPause()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "Pause", "" } });
        }

        public static void SendLoop(Playlist playlist)
        {
            string loopXML = XmlConverter.Serialize(playlist.Loop);
            ValueSet valueSet = new ValueSet();

            valueSet.Add("Loop", playlist.PlaylistIndex.ToString());
            valueSet.Add("Kind", loopXML);

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendShuffle(Playlist playlist)
        {
            string shuffleXML = XmlConverter.Serialize(playlist.Shuffle);
            string shuffleListXML = XmlConverter.Serialize(playlist.ShuffleList);
            ValueSet valueSet = new ValueSet();

            valueSet.Add("Shuffle", playlist.PlaylistIndex.ToString());
            valueSet.Add("Kind", shuffleXML);
            valueSet.Add("List", shuffleListXML);

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendPlaylistXML(Playlist playlist)
        {
            int playlistIndex = playlist.PlaylistIndex;
            ValueSet valueSet = new ValueSet();
            valueSet.Add("PlaylistXML", playlistIndex.ToString());
            valueSet.Add("XML", XmlConverter.Serialize(Library.Current[playlistIndex]));

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendGetXmlText()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "GetXmlText", "" } });
        }

        public static void SendLoadXML(string xmlText)
        {
            ValueSet valueSet = new ValueSet();

            valueSet.Add("LoadXML", xmlText);

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendSongXML(Song song)
        {
            int playlistIndex = Library.Current.CurrentPlaylistIndex;
            int songsIndex = Library.Current.CurrentPlaylist.Songs.IndexOf(song);

            ValueSet valueSet = new ValueSet();
            valueSet.Add("SongXML", string.Format("{0};{1}", playlistIndex, songsIndex));
            valueSet.Add("XML", XmlConverter.Serialize(song));

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendSongXML(int playlistIndex, int songsIndex)
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add("SongXML", string.Format("{0};{1}", playlistIndex, songsIndex));
            valueSet.Add("XML", XmlConverter.Serialize(Library.Current[playlistIndex][songsIndex]));

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendRemoveSong(int playlistIndex, int songsIndex)
        {
            ValueSet valueSet = new ValueSet { { "RemoveSong", string.Format("{0};{1}", playlistIndex, songsIndex) } };
            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendRemovePlaylist(Playlist playlist)
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "RemovePlaylist", playlist.PlaylistIndex.ToString() } });
        }

        public static void SetReceivedEvent()
        {
            BackgroundMediaPlayer.MessageReceivedFromBackground += MessageReceivedFromBackground;
        }

        private static void MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            ValueSet valueSet = e.Data;

            foreach (string key in valueSet.Keys)
            {
                switch (key)
                {
                    case "SongsIndex":
                        GetSongsIndex(valueSet);
                        return;

                    case "SongsIndexAndShuffle":
                        GetSongsIndexAndShuffle(valueSet);
                        return;

                    case "XmlText":
                        GetXmlText(valueSet);
                        return;

                    case "Pause":
                        ViewModel.Current.UpdatePlayPauseIconAndText();
                        return;

                    case "Skip":
                        SkipSongs.AskAboutSkippedSong();
                        return;
                }
            }
        }

        private static void GetSongsIndex(ValueSet valueSet)
        {
            int songsIndex = int.Parse(valueSet["SongsIndex"].ToString());

            ViewModel.Current.CurrentPlaylist.SongsIndex = songsIndex;
        }

        private static void GetSongsIndexAndShuffle(ValueSet valueSet)
        {
            int songsIndex = int.Parse(valueSet["SongsIndexAndShuffle"].ToString());
            ShuffleKind shuffle = XmlConverter.Deserialize<ShuffleKind>(valueSet["ShuffleKind"].ToString());
            List<int> shuffleList = XmlConverter.Deserialize<List<int>>(valueSet["ShuffleList"].ToString());

            ViewModel.Current.CurrentPlaylist.Shuffle = shuffle;

            ViewModel.Current.CurrentPlaylist.ShuffleList = shuffleList;
            ViewModel.Current.CurrentPlaylist.UpdateSongsAndShuffleListSongs();

            ViewModel.Current.CurrentPlaylist.SongsIndex = songsIndex;
        }

        private static void GetXmlText(ValueSet valueSet)
        {
            Library.Current.Load(valueSet["XmlText"].ToString());
        }
    }
}
