using FolderMusicUwpLib;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryLib
{
    public class SkipSongs
    {
        private static string skipSongsFileName = "SkipSongs.xml";

        private List<Song> allSkipSongs;
        private List<Song> skipSkipSongs;

        public bool HaveSongs { get { return Songs.Count > 0; } }

        public Song CurrentSong { get { return  GetCurrentSong(); } }

        public List<Song> Songs { get { return GetAllNotSkipSongs(); } }

        private SkipSongs()
        {
            allSkipSongs = new List<Song>();
            skipSkipSongs = new List<Song>();
        }

        public async static Task<SkipSongs> GetNew()
        {
            SkipSongs obj = new SkipSongs();

            await obj.SetAllSkipSongs();

            return obj;
        }

        private Song GetCurrentSong()
        {
            List<Song> songs = Songs;

            if (songs.Count == 0) return new Song();

            return songs[0];
        }

        private List<Song> GetAllNotSkipSongs()
        {
            List<Song> list = new List<Song>(allSkipSongs);

            foreach (Song skipSkipSong in skipSkipSongs)
            {
                if (list.Contains(skipSkipSong)) list.Remove(skipSkipSong);
            }

            return list;
        }

        public async Task Yes_Click()
        {
            foreach (Playlist playlist in Library.Current.Playlists)
            {
                if (playlist.Songs.Contains(CurrentSong))
                {
                    playlist.RemoveSong(playlist.Songs.IndexOf(CurrentSong));
                    break;
                }
            }

            await SetAllSkipSongs();
            await RemoveSkipSongAndSave(CurrentSong);
        }

        public async Task No_Click()
        {
            BackgroundCommunicator.SendSongXML(CurrentSong);

            await SetAllSkipSongs();
            await RemoveSkipSongAndSave(CurrentSong);
        }

        public async Task Skip_Click()
        {
            await SetAllSkipSongs();
            skipSkipSongs.Add(CurrentSong);
        }

        public async Task RemoveSkipSongAndSave(Song song)
        {
            for (int i = 0; i < allSkipSongs.Count; i++)
            {
                if (allSkipSongs[i].Path == song.Path) allSkipSongs.RemoveAt(i);
            }

            await SaveSkipSongs(allSkipSongs);
        }

        public async Task SetAllSkipSongs()
        {
            try
            {
                List<Song> loadSongs = await LoadSkipSongs();

                DeleteNotExistingSongs(loadSongs);
            }
            catch { }
        }

        private async static Task<List<Song>> LoadSkipSongs()
        {
            try
            {
                return await LibraryIO.LoadObject<List<Song>>(skipSongsFileName);
            }
            catch { }

            return new List<Song>();
        }

        private void DeleteNotExistingSongs(List<Song> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!IsSongInAnyPlaylist(list[i])) list.RemoveAt(0);
            }
        }

        private bool IsSongInAnyPlaylist(Song skipSong)
        {
            Song[] songs;

            for (int i = 0; i < Library.Current.Length; i++)
            {
                songs = Library.Current[i].Songs.Where(x => x.Path == skipSong.Path).ToArray();

                if (songs.Length == 1)
                {
                    System.Diagnostics.Debug.WriteLine(songs[0]);
                    if (!allSkipSongs.Contains(songs[0])) allSkipSongs.Add(songs[0]);

                    return true;
                }
            }

            return false;
        }

        public async static Task<bool> SkipSongsExists()
        {
            return (await LoadSkipSongs()).Count > 0;
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

        private async static Task SaveSkipSongs(List<Song> list)
        {
            await LibraryIO.SaveObject(list, skipSongsFileName);
        }

        public async static Task Delete()
        {
            await LibraryIO.Delete(skipSongsFileName);
        }
    }
}
