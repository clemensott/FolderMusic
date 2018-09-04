using MusicPlayer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderMusicConsoleTest
{
    class Program
    {
        static ILibrary library;
        static SkipSongs skipSongs;

        static void Main(string[] args)
        {
            library = new Library();
            skipSongs = new SkipSongs(library);
            //skipSongs.Delete();

            SkipSongs(10);

            Console.WriteLine();
            foreach (Song s in skipSongs.GetSongs())
            {
                Console.WriteLine(s.Path);
            }

            Console.WriteLine();
            foreach (SkipSong ss in skipSongs)
            {
                Console.WriteLine(ss.Song.Path);
                ss.Handle = ProgressType.Remove;
            }

            Console.WriteLine();
            Console.WriteLine(skipSongs.GetSongs().Count());
            foreach (Song s in skipSongs.GetSongs())
            {
                Console.WriteLine(s.Path);
            }

            Console.WriteLine(library.Playlists.SelectMany(p => p.Songs).Count());

            Console.ReadLine();
        }

        private static void SkipSongs(int count)
        {
            Random ran = new Random();

            for (int i = 0; i < count; i++)
            {
                int index = ran.Next(library.Playlists.SelectMany(p => p.Songs).Count());
                Song songToSkip = library.Playlists.SelectMany(p => p.Songs).ElementAt(index);
                skipSongs.Add(songToSkip);

                Console.WriteLine(songToSkip.Path);
            }
        }
    }
}
