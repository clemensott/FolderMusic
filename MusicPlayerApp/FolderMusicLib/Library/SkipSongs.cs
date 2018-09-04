using FolderMusicLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace LibraryLib
{
    public class SkipSongs
    {
        private static volatile bool IsAskingSkipSong = false;
        private static int playlistIndex, songsIndex;
        private static string skipSongsFileName = "SkipSongs.xml";

        public static async Task AskAboutSkippedSong()
        {
            if (IsAskingSkipSong || Library.Current.IsEmpty) return;
            IsAskingSkipSong = true;

            MessageDialog messageDialog;
            List<Song> list = await LoadSkipSongs();
            int listCount = list.Count;

            FindExistingSong(list);

            if (list.Count == 0)
            {
                await LibraryIO.Delete(skipSongsFileName);
                IsAskingSkipSong = false;
                return;
            }

            await SaveListWhenCountsAreDiffrent(list, listCount, list.Count);

            messageDialog = GetMessageDialog(list[0].Path);

            await CoreApplication.MainView.CoreWindow.Dispatcher.
                RunAsync(CoreDispatcherPriority.Normal, async () =>
                { await messageDialog.ShowAsync(); });
        }

        private static void FindExistingSong(List<Song> list)
        {
            while (list.Count > 0 && !IsSongInAnyPlaylist(list[0]))
            {
                list.RemoveAt(0);
            }
        }

        private async static Task SaveListWhenCountsAreDiffrent(List<Song> list, int count1, int count2)
        {
            if (count1 == count2) return;

            await RemoveSkipSongAndSave(list, new Song());
        }

        private static MessageDialog GetMessageDialog(string path)
        {
            string content = GetMessageDialogContent(path);
            MessageDialog messageDialog = new MessageDialog(content);

            try
            {
                messageDialog.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(CommandHandlers)));
                messageDialog.Commands.Add(new UICommand("No", new UICommandInvokedHandler(CommandHandlers)));
            }
            catch { }

            return messageDialog;
        }

        private static string GetMessageDialogContent(string path)
        {
            return "Couldn't play following song. Do you want to remove it?\n" + path;
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

                    if (!same) break;

                    playlist.UpdateSongsAndShuffleListSongs();
                    playlist.UpdateCurrentSong();
                    break;
            }

            skipSongs = await LoadSkipSongs();
            saveSongsCount = skipSongs.Count;

            await RemoveSkipSongAndSave(skipSongs, song);
            IsAskingSkipSong = false;

            if (saveSongsCount == 1)
            {
                await Library.Current.SaveAsync();
                return;
            }

            AskAboutSkippedSong();
        }

        private static bool IsSongInAnyPlaylist(Song skipSong)
        {
            Song[] songs;

            for (int i = 0; i < Library.Current.Length; i++)
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

        public async static Task AddSkipSongAndSave(Song song)
        {
            List<Song> list = await LoadSkipSongs();

            foreach (Song saveSong in list)
            {
                if (saveSong.Path == song.Path) return;
            }

            list.Add(song);

            await SaveSkipSongs(list);
        }

        public async static Task RemoveSkipSongAndSave(List<Song> list, Song saveSong)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Path == saveSong.Path) list.RemoveAt(i);
            }

            await SaveSkipSongs(list);
        }

        private async static Task SaveSkipSongs(List<Song> list)
        {
            await LibraryIO.SaveObject(list, skipSongsFileName);
        }

        public async static Task<List<Song>> LoadSkipSongs()
        {
            try
            {
                return await LibraryIO.LoadObject<List<Song>>(skipSongsFileName);
            }
            catch { }

            return new List<Song>();
        }
    }
}
