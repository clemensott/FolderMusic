using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace FolderMusic.FrameHistory
{
    public class RestoreMusicProperties
    {
        public string Album { get; set; }

        public string AlbumArtist { get; set; }

        public string Artist { get; set; }

        public string[] Composers { get; set; }

        public string[] Conductors { get; set; }

        public string[] Genre { get; set; }

        public string[] Producers { get; set; }

        public string Publisher { get; set; }

        public uint Rating { get; set; }

        public string Subtitle { get; set; }

        public string Title { get; set; }

        public uint TrackNumber { get; set; }

        public string[] Writers { get; }

        public uint Year { get; set; }

        public RestoreMusicProperties()
        {
        }

        public RestoreMusicProperties(MusicProperties mp)
        {
            Album = mp.Album;
            AlbumArtist = mp.AlbumArtist;
            Artist = mp.Artist;
            Composers = mp.Composers.ToArray();
            Conductors = mp.Conductors.ToArray();
            Genre = mp.Genre.ToArray();
            Producers = mp.Producers.ToArray();
            Publisher = mp.Publisher;
            Rating = mp.Rating;
            Subtitle = mp.Subtitle;
            Title = mp.Title;
            TrackNumber = mp.TrackNumber;
            Writers = mp.Writers.ToArray();
        }

        public async Task<MusicProperties> ToMusicProperties(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            MusicProperties mp = await file.Properties.GetMusicPropertiesAsync();

            mp.Album = Album;
            mp.AlbumArtist = AlbumArtist;
            mp.Artist = Artist;
            mp.Publisher = Publisher;
            mp.Rating = Rating;
            mp.Subtitle = Subtitle;
            mp.Title = Title;
            mp.TrackNumber = TrackNumber;
            mp.Year = Year;

            CopyTo(mp.Composers, Composers);
            CopyTo(mp.Conductors, Conductors);
            CopyTo(mp.Genre, Genre);
            CopyTo(mp.Producers, Producers);
            CopyTo(mp.Writers, Writers);

            return mp;
        }

        private void CopyTo(IList<string> list, string[] array)
        {
            list.Clear();

            foreach (string item in array) list.Add(item);
        }
    }
}
