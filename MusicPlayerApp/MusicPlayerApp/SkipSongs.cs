using LibraryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace MusicPlayerApp
{
    public class SkipSongs
    {
        private static volatile bool IsAskingSkipSong = false;
        private static Song skipSong;

        public static async Task AskAboutSkippedSong()
        {
            if (IsAskingSkipSong) return;
            IsAskingSkipSong = true;

            string dialogContent = "";
            MessageDialog messageDialog;
            List<Song> list = await Library.LoadSkipSongs();

            if (list.Count == 0)
            {
                IsAskingSkipSong = false;
                return;
            }

            skipSong = list[0];

            dialogContent = "Couldn't play following Song. Do you want to remove this Song from the Playlist?\n";
            dialogContent += skipSong.Path;

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
            bool same;
            int playlistIndex, songsIndex, saveSongsCount;
            string actions = commandLabel.Label;
            List<Song> skipSongs;
            Playlist playlist;

            switch (actions)
            {
                case "No":
                    break;

                case "Yes":
                    if (!IsSongInAnyPlaylist(out playlist, out songsIndex)) break;

                    same = Library.Current.CurrentPlaylist == playlist;
                    playlistIndex = Library.Current.GetPlaylistIndex(playlist);

                    Library.Current.RemoveSongFromPlaylist(playlist, songsIndex);
                    BackgroundCommunicator.SendRemoveSong(playlistIndex, songsIndex);

                    if (same) break;

                    UiUpdate.CurrentPlaylistSongs();
                    UiUpdate.ShuffleListIndex();
                    break;
            }

            skipSongs = await Library.LoadSkipSongs();
            saveSongsCount = skipSongs.Count;

            await Library.RemoveSkipSongAndSave(skipSongs, skipSong);
            IsAskingSkipSong = false;

            if (saveSongsCount == 1)
            {
                await Library.Current.SaveAsync();
                return;
            }

            await AskAboutSkippedSong();
        }

        private static bool IsSongInAnyPlaylist(out Playlist playlist, out int songsIndex)
        {
            songsIndex = -1;

            return HaveAnyPlaylistSkipSongPath(out playlist) && IsSongInPlaylist(playlist, out songsIndex);
        }

        private static bool HaveAnyPlaylistSkipSongPath(out Playlist playlist)
        {
            string folderPath = System.IO.Path.GetDirectoryName(skipSong.Path);
            Playlist[] playlists = Library.Current.GetPlaylists().Where(x => x.AbsolutePath == folderPath).ToArray();

            playlist = playlists.Length == 1 ? playlists[0] : null;
            return playlists.Length == 1;
        }

        private static bool IsSongInPlaylist(Playlist playlist, out int songsIndex)
        {
            Song[] songs = playlist.Songs.Where(x => x.Path == skipSong.Path).ToArray();

            songsIndex = songs.Length == 1 ? playlist.Songs.IndexOf(songs[0]) : -1;
            return songs.Length == 1;
        }
    }
}
