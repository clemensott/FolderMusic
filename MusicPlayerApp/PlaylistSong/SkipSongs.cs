using LibraryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace MusicPlayerLib
{
    public class SkipSongs
    {
        private static volatile bool IsAskingSkipSong = false;
        private static int playlistIndex, songsIndex;

        public static async Task AskAboutSkippedSong()
        {
            if (IsAskingSkipSong || Library.Current.IsEmpty) return;
            IsAskingSkipSong = true;

            string dialogContent = "";
            MessageDialog messageDialog;
            List<Song> list = await Library.LoadSkipSongs();

            while (list.Count > 0 && !IsSongInAnyPlaylist(list[0]))
            {
                list.RemoveAt(0);
            }

            await Library.RemoveSkipSongAndSave(list, new Song());

            if (list.Count == 0)
            {
                IsAskingSkipSong = false;
                return;
            }

            dialogContent = "Couldn't play following Song. Do you want to remove this Song from the Playlist?\n";
            dialogContent += list[0].Path;

            messageDialog = new MessageDialog(dialogContent);

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

        private async static void CommandHandlers(IUICommand commandLabel)
        {
            int saveSongsCount;
            string actions = commandLabel.Label;
            Song song = Library.Current[playlistIndex][songsIndex];
            List<Song> skipSongs;
            Playlist playlist = Library.Current[playlistIndex];
            bool same = Library.Current.CurrentPlaylist == playlist;

            switch (actions)
            {
                case "No":
                    BackgroundCommunicator.SendSongXML(playlistIndex, songsIndex);
                    break;

                case "Yes":
                    Library.Current.RemoveSongFromPlaylist(playlist, songsIndex);
                    BackgroundCommunicator.SendRemoveSong(playlistIndex, songsIndex);

                    if (!same) break;

                    playlist.UpdateSongsAndShuffleListSongs();
                    playlist.UpdateCurrentSong();
                    break;
            }

            skipSongs = await Library.LoadSkipSongs();
            saveSongsCount = skipSongs.Count;

            await Library.RemoveSkipSongAndSave(skipSongs, Library.Current[playlistIndex][songsIndex]);
            IsAskingSkipSong = false;

            if (saveSongsCount == 1)
            {
                await Library.Current.SaveAsync();
                return;
            }

            await AskAboutSkippedSong();
        }

        private static bool IsSongInAnyPlaylist(Song skipSong)
        {
            Song[] songs;

            for (int i=0;i<Library.Current.Length;i++)
            {
                songs = Library.Current[i].Songs.Where(x => x.Path == skipSong.Path).ToArray();

                if (songs.Length == 1)
                {
                    playlistIndex = i;
                    songsIndex = Library.Current[playlistIndex].Songs.IndexOf(songs[0]);

                    return true;
                }
            }

            playlistIndex = -1;
            songsIndex = -1;

            return false;
        }
    }
}
