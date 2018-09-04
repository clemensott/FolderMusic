using Windows.Foundation.Collections;
using Windows.Media.Playback;
using System.Collections.Generic;
using LibraryLib;

namespace MusicPlayerApp
{
    class BackgroundCommunicator
    {
        public static void SendPlaySong(int playlistIndex, int songsIndex)
        {
            ValueSet valueSet = new ValueSet { { "PlaySong", string.Format("{0};{1}", playlistIndex, songsIndex) } };
            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendCurrentPlaylistIndex(bool play)
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add("CurrentPlaylistIndex", Library.Current.CurrentPlaylistIndex.ToString());
            valueSet.Add("DoPlay", play.ToString());

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

        public static void SendPrevious()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "Previous", "" } });
        }

        public static void SendNext()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "Next", "" } });
        }

        public static void SendLoop(int playlistIndex)
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "Loop", playlistIndex.ToString() } });
        }

        public static void SendShuffle(int playlistIndex)
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "Shuffle", playlistIndex.ToString() } });
        }

        public static void SendPlaylistPageTap(int songsIndex)
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add("PlaylistPageTap", string.Format("{0};{1}", App.ViewModel.OpenPlaylistIndex, songsIndex));

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendPlaylistXML(Playlist playlist)
        {
            int playlistIndex = Library.Current.GetPlaylistIndex(playlist);
            ValueSet valueSet = new ValueSet();
            valueSet.Add("PlaylistXML", playlistIndex.ToString());
            valueSet.Add("XML", XmlConverter.Serialize(Library.Current[playlistIndex]));

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendGetXmlText()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "GetXmlText", "" } });
        }

        public static void SendLoadXML(bool fix, string xmlText)
        {
            ValueSet valueSet = new ValueSet();

            valueSet.Add("LoadXML", xmlText);
            valueSet.Add("Fix", fix.ToString());

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendSong(Song song)
        {
            int playlistIndex = Library.Current.CurrentPlaylistIndex;
            int songsIndex = Library.Current.CurrentPlaylist.Songs.IndexOf(song);

            ValueSet valueSet = new ValueSet();
            valueSet.Add("RefreshSong", string.Format("{0};{1}", playlistIndex, songsIndex));
            valueSet.Add("XML", XmlConverter.Serialize(song));

            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendRemoveSong(int playlistIndex, int songsIndex)
        {
            ValueSet valueSet = new ValueSet { { "RemoveSong", string.Format("{0};{1}", playlistIndex, songsIndex) } };
            BackgroundMediaPlayer.SendMessageToBackground(valueSet);
        }

        public static void SendRemovePlaylist(Playlist playlist)
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "RemovePlaylist", Library.Current.GetPlaylistIndex(playlist).ToString() } });
        }

        public static void SetReceivedEvent()
        {
            BackgroundMediaPlayer.MessageReceivedFromBackground += MessageReceivedFromBackground;
        }

        private static void MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            bool same;
            int songsIndex;
            List<int> shuffleList;
            double position, duration;
            string shuffleKindXml, currentSongPath;
            ShuffleKind shuffle;
            Playlist playlist;
            ValueSet valueSet = e.Data;

            foreach (string key in valueSet.Keys)
            {
                switch (key)
                {
                    case "Current":
                        songsIndex = int.Parse(valueSet[key].ToString());
                        position = double.Parse(valueSet["Position"].ToString());
                        duration = double.Parse(valueSet["Duration"].ToString());

                        same = Library.Current.CurrentPlaylist.SongsIndex == songsIndex &&
                            Library.Current.CurrentPlaylist.SongPositionMilliseconds == position &&
                            Library.Current.CurrentSong.NaturalDurationMilliseconds == duration;

                        Library.Current.CurrentPlaylist.SongsIndex = songsIndex;
                        Library.Current.CurrentPlaylist.SongPositionMilliseconds = position;
                        Library.Current.CurrentSong.NaturalDurationMilliseconds = duration;

                        if (!same) UiUpdate.CurrentSongTitleArtistNaturalDuration();
                        return;

                    case "XmlText":
                        Library.Current.Load(valueSet[key].ToString());
                        App.ViewModel.SetChangedCurrentPlaylistIndex();
                        UiUpdate.PlaylistsAndCurrentPlaylist();

                        if (PlaylistPage.Open) PlaylistPage.Current.UpdateUi();
                        return;

                    case "Shuffle":
                        currentSongPath = Library.Current.CurrentSong.Path;
                        playlist = PlaylistPage.Open ? App.ViewModel.OpenPlaylist : App.ViewModel.CurrentPlaylist;
                        shuffleKindXml = valueSet["Kind"].ToString();

                        shuffleList = XmlConverter.Deserialize<List<int>>(valueSet[key].ToString());
                        shuffle = XmlConverter.Deserialize<ShuffleKind>(shuffleKindXml);
                        same = playlist.Shuffle == shuffle;

                        if (ShuffleKind.Complete == shuffle)
                        {
                            Library.Current.CurrentPlaylist.ShuffleListIndex = Library.Current.CurrentPlaylist.ShuffleListIndex;
                        }

                        playlist.SetShuffle(shuffle, shuffleList);
                        UiUpdate.CurrentPlaylistSongs();

                        if (Library.Current.CurrentSong.Path == currentSongPath) UiUpdate.CurrentSongTitleArtistNaturalDuration();
                        if (!same) UiUpdate.ShuffleIcon();
                        if (PlaylistPage.Open) PlaylistPage.Current.UpdateUi();
                        return;

                    case "Pause":
                        UiUpdate.PlayPauseIcon();
                        return;

                    case "Skip":
                        SkipSongs.AskAboutSkippedSong();
                        return;
                }
            }
        }
    }
}
