using FolderMusicLib;
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

        public Song CurrentSong { get { return GetCurrentSong(); } }

        public List<Song> Songs { get { return GetAllNotSkipSongs(); } }

        private SkipSongs()
        {
            allSkipSongs = new List<Song>();
            skipSkipSongs = new List<Song>();
        }

        public static SkipSongs GetNew()
        {
            SkipSongs obj = new SkipSongs();

            obj.SetAllSkipSongs();

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

        public void Yes_Click()
        {
            foreach (Playlist playlist in Library.Current.Playlists)
            {
                if (playlist.Songs.Contains(CurrentSong))
                {
                    playlist.RemoveSong(playlist.Songs.IndexOf(CurrentSong));
                    break;
                }
            }

            SetAllSkipSongs();
            RemoveSkipSongAndSave(CurrentSong);
        }

        public void No_Click()
        {
            BackgroundCommunicator.SendSongXML(CurrentSong);

            SetAllSkipSongs();
            RemoveSkipSongAndSave(CurrentSong);
        }

        public void Skip_Click()
        {
            SetAllSkipSongs();
            skipSkipSongs.Add(CurrentSong);
        }

        public void RemoveSkipSongAndSave(Song song)
        {
            for (int i = 0; i < allSkipSongs.Count; i++)
            {
                if (allSkipSongs[i].Path == song.Path) allSkipSongs.RemoveAt(i);
            }

            SaveSkipSongs(allSkipSongs);
        }

        public void SetAllSkipSongs()
        {
            try
            {
                List<Song> loadSongs = LoadSkipSongs();

                DeleteNotExistingSongs(loadSongs);
            }
            catch { }
        }

        private static List<Song> LoadSkipSongs()
        {
            try
            {
                return LibraryIO.LoadObject<List<Song>>(skipSongsFileName);
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

        public static bool SkipSongsExists()
        {
            return LoadSkipSongs().Count > 0;
        }

        public static void AddSkipSongAndSave(Song song)
        {
            List<Song> list = LoadSkipSongs();

            foreach (Song saveSong in list)
            {
                if (saveSong.Path == song.Path) return;
            }

            list.Add(song);

            SaveSkipSongs(list);
        }

        private static void SaveSkipSongs(List<Song> list)
        {
            LibraryIO.SaveObject(list, skipSongsFileName);
        }

        public static void Delete()
        {
            LibraryIO.Delete(skipSongsFileName);
        }
    }
}
