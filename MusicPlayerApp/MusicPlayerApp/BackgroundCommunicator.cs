using System;
using PlaylistSong;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace MusicPlayerApp
{
    public class BackgroundCommunicator
    {
        private static int skipIndex = -1;

        public static void SendPlaySong(int index)
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "PlaySong", index.ToString() } });
        }

        public static void SendCurrentPlaylistIndex(bool play)
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "CurrentPlaylistIndex",
                    Library.Current.CurrentPlaylistIndex.ToString() }, { "Play", play.ToString() } });
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

        public static void SendLoop(int index)
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "Loop", index.ToString() } });
        }

        public static void SendShuffle(int index)
        {
            BackgroundMediaPlayer.SendMessageToBackground(Library.Current[index].GetShuffleAsValueSet(index));
        }

        public static void SendPlaylistPageTap(bool fromShuffleList, int songsIndex)
        {
            BackgroundMediaPlayer.SendMessageToBackground(
                new ValueSet { { "PlaylistPageTap", App.ViewModel.OpenPlaylistIndex.ToString() },
                    { "CurrentSongIndex", songsIndex.ToString() },
                    { "FromShuffleList", fromShuffleList.ToString() } });
        }

        public static void SendRun()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "Run", "" } });
        }

        public static void SendGetCurrent()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "GetCurrent", "" } });
        }

        public static void SendLoad()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "Load", "" } });
        }

        public static void SendRemoveSong(int index)
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "RemoveSong", index.ToString() } });
        }

        public static void SendRemovePlaylist(int index)
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "RemovePlaylist", index.ToString() } });
        }

        public static void SetReceivedEvent()
        {
            BackgroundMediaPlayer.MessageReceivedFromBackground += MessageReceivedFromBackground;
        }

        private static void MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            ValueSet valueSet;

            try
            {
                valueSet = e.Data;
            }
            catch
            {
                return;
            }

            try
            {
                foreach (string key in valueSet.Keys)
                {
                    switch (key)
                    {
                        case "Current":
                            int index = int.Parse(valueSet[key].ToString());
                            double position = double.Parse(valueSet["Position"].ToString());
                            double duration = double.Parse(valueSet["Duration"].ToString());

                            bool same = Library.Current.CurrentPlaylist.CurrentSongIndex == index &&
                                Library.Current.CurrentPlaylist.SongPositionMilliseconds == position &&
                                Library.Current.CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds == duration;

                            Library.Current.CurrentPlaylist.CurrentSongIndex = index;
                            Library.Current.CurrentPlaylist.SongPositionMilliseconds = position;
                            Library.Current.CurrentPlaylist.CurrentSong.NaturalDurationMilliseconds = duration;

                            if (!same) UiUpdate.CurrentSongTitleArtistNaturalDuration();
                            return;

                        case "New":
                            bool first = bool.Parse(valueSet[key].ToString());
                            string pathNew = valueSet["Path"].ToString();

                            Library.Current.CurrentPlaylist.AddShuffleCompleteSong(first, pathNew);

                            UiUpdate.CurrentPlaylistSongsAndIndex();
                            UiUpdate.CurrentSongTitleArtistNaturalDuration();
                            return;

                        case "Skip":
                            skipIndex = int.Parse(valueSet[key].ToString());

                            AskAboutSkippedSong();
                            return;

                        case "Shuffle":
                            Library.Current.CurrentPlaylist.SetShuffleList(valueSet);

                            UiUpdate.CurrentPlaylistSongsAndIndex();
                            UiUpdate.ShuffleIcon();
                            UiUpdate.CurrentSongTitleArtistNaturalDuration();
                            return;

                        case "Pause":
                            UiUpdate.PlayPauseIcon();
                            return;
                    }
                }
            }
            catch { }
        }

        public static async void AskAboutSkippedSong()
        {
            string dialogContent = "Couldn't play following Song. Do you want to remove this Song from the Playlist?\n";
            dialogContent += Library.Current.CurrentPlaylist[skipIndex].Path;

            MessageDialog messageDialog = new MessageDialog(dialogContent);

            try
            {
                messageDialog.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(CommandHandlers)));
                messageDialog.Commands.Add(new UICommand("No", new UICommandInvokedHandler(CommandHandlers)));
            }
            catch { }

            await CoreApplication.MainView.CoreWindow.Dispatcher.
                RunAsync(CoreDispatcherPriority.Normal, async () =>
                { await messageDialog.ShowAsync(); });
        }

        private static void CommandHandlers(IUICommand commandLabel)
        {
            var Actions = commandLabel.Label;
            switch (Actions)
            {
                case "Yes":
                    Library.Current.CurrentPlaylist.RemoveSong(skipIndex);
                    SendRemoveSong(skipIndex);

                    UiUpdate.CurrentPlaylistSongsAndIndex();
                    UiUpdate.CurrentSongIndex();

                    Library.SaveAsync();
                    break;

                case "No":

                    break;
            }
        }
    }
}
