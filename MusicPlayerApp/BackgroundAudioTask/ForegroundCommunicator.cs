using LibraryLib;
using Windows.Foundation.Collections;
using Windows.Media.Playback;

namespace BackgroundAudioTask
{
    class ForegroundCommunicator
    {
        private static Song CurrentSong { get { return Library.Current.CurrentSong; } }

        public static void SendPause()
        {
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { "Pause", "" } });
        }

        public static void SendCurrent()
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add("Current", Library.Current.CurrentPlaylist.SongsIndex.ToString());
            valueSet.Add("Position", Library.Current.CurrentSongPositionMilliseconds.ToString());
            valueSet.Add("Duration", CurrentSong.NaturalDurationMilliseconds.ToString());

            BackgroundMediaPlayer.SendMessageToForeground(valueSet);
        }

        public static void SendXmlText()
        {
            if (Library.IsLoaded)
            {
                BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { "XmlText", Library.Current.GetXmlText() } });
            }
        }

        public async static void SendSkip()
        {
            await Library.AddSkipSongAndSave(CurrentSong);
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { "Skip", "" } });
        }

        public static void SendShuffle(int playlistIndex)
        {
            string xmlText = XmlConverter.Serialize(Library.Current[playlistIndex].ShuffleList);
            string shuffleKindXml = XmlConverter.Serialize(Library.Current[playlistIndex].Shuffle);
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet { { "Shuffle", xmlText }, { "Kind", shuffleKindXml } });
        }

        public async static void MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            int playlistIndex, songsIndex;
            string currentSongPath;
            string[] parts;
            ValueSet valueSet = e.Data;

            foreach (string key in valueSet.Keys)
            {
                switch (key)
                {
                    case "PlaySong":
                        parts = valueSet[key].ToString().Split(';');
                        playlistIndex = int.Parse(parts[0]);
                        songsIndex = int.Parse(parts[1]);

                        Library.Current.CurrentPlaylistIndex = playlistIndex;
                        Library.Current.CurrentPlaylist.SongsIndex = songsIndex;

                        BackgroundAudioTask.Current.PlayCurrentSong(true);
                        return;

                    case "Play":
                        BackgroundAudioTask.Current.Play();
                        return;

                    case "Pause":
                        BackgroundAudioTask.Current.Pause();
                        return;

                    case "Previous":
                        BackgroundAudioTask.Current.Previous();
                        return;

                    case "Next":
                        BackgroundAudioTask.Current.Next(BackgroundAudioTask.Current.IsPlaying);
                        return;

                    case "Loop":
                        playlistIndex = int.Parse(valueSet[key].ToString());
                        Library.Current[playlistIndex].SetNextLoop();
                        BackgroundAudioTask.Current.SetLoopToBackgroundPlayer();

                        await Library.Current.SaveAsync();
                        return;

                    case "Shuffle":
                        playlistIndex = int.Parse(valueSet[key].ToString());
                        Library.Current[playlistIndex].SetNextShuffle();

                        SendShuffle(playlistIndex);
                        await Library.Current.SaveAsync();
                        return;

                    case "CurrentPlaylistIndex":
                        Library.Current.CurrentPlaylistIndex = int.Parse(valueSet[key].ToString());
                        BackgroundAudioTask.Current.PlayCurrentSong(bool.Parse(valueSet["DoPlay"].ToString()));

                        await Library.Current.SaveAsync();
                        return;

                    case "GetXmlText":
                        SendXmlText();
                        return;

                    case "PlaylistXML":
                        currentSongPath = CurrentSong.Path;

                        playlistIndex = int.Parse(valueSet[key].ToString());
                        Library.Current[playlistIndex] = XmlConverter.Deserialize<Playlist>(valueSet["XML"].ToString());
                        PlaySongIfOther(currentSongPath);

                        await Library.Current.SaveAsync();
                        return;

                    case "LoadXML":
                        currentSongPath = CurrentSong.Path;

                        if (Library.IsLoaded || bool.Parse(valueSet["Fix"].ToString())) Library.Current.Load(valueSet[key].ToString());
                        PlaySongIfOther(currentSongPath);

                        await Library.Current.SaveAsync();
                        return;

                    case "PlaylistPageTap":
                        parts = valueSet[key].ToString().Split(';');
                        Library.Current.CurrentPlaylistIndex = int.Parse(parts[0]);
                        songsIndex = int.Parse(parts[1]);

                        Library.Current.CurrentPlaylist.SongsIndex = songsIndex;
                        BackgroundAudioTask.Current.PlayCurrentSong(true);

                        await Library.Current.SaveAsync();
                        return;

                    case "Song":
                        currentSongPath = CurrentSong.Path;

                        parts = valueSet[key].ToString().Split(';');
                        playlistIndex = int.Parse(parts[0]);
                        songsIndex = int.Parse(parts[1]);

                        Library.Current[playlistIndex][songsIndex] = XmlConverter.Deserialize<Song>(valueSet["XML"].ToString());
                        PlaySongIfOther(currentSongPath);

                        await Library.Current.SaveAsync();
                        return;

                    case "RemoveSong":
                        currentSongPath = CurrentSong.Path;

                        parts = valueSet[key].ToString().Split(';');
                        playlistIndex = int.Parse(parts[0]);
                        songsIndex = int.Parse(parts[1]);

                        Library.Current.RemoveSongFromPlaylist(Library.Current[playlistIndex], songsIndex);
                        PlaySongIfOther(currentSongPath);

                        await Library.Current.SaveAsync();
                        return;

                    case "RemovePlaylist":
                        currentSongPath = CurrentSong.Path;

                        Library.Current.DeleteAt(int.Parse(valueSet[key].ToString()));
                        PlaySongIfOther(currentSongPath);

                        await Library.Current.SaveAsync();
                        return;
                }
            }
        }

        private static void PlaySongIfOther(string path)
        {
            if (path != CurrentSong.Path) BackgroundAudioTask.Current.PlayCurrentSong(BackgroundAudioTask.Current.IsPlaying);
        }
    }
}
