using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace PlaylistSong
{
    public class Song
    {
        private bool isEmpty;
        private double naturalDurationMilliseconds = 1;
        private string title, artist;
        private Uri uri;

        public bool IsEmpty { get { return isEmpty; } }

        public double NaturalDurationMilliseconds
        {
            get { return naturalDurationMilliseconds; }
            set { naturalDurationMilliseconds = value; }
        }

        public string Title { get { return title; } }

        public string Artist { get { return artist; } }

        public string Path { get { return uri != null ? uri.OriginalString : ""; } }

        public Uri Uri { get { return uri; } }

        public Brush TextBrush { get { return Playlist.TextBrush; } }

        public Visibility ArtistVisibility { get { return artist != "" ? Visibility.Visible : Visibility.Collapsed; } }

        public Song()
        {
            isEmpty = true;
            title = "Empty";
            artist = "";
        }

        public Song(string absolutePath)
        {
            uri = new Uri(absolutePath, UriKind.Absolute);
            SetTitelAndArtist();
        }

        public Song(string keyTitle, string pathArtist)
        {
            int semicolonTitleIndex = keyTitle.IndexOf(";");
            int semicolonArtistIndex = pathArtist.IndexOf(";");

            uri = new Uri(semicolonArtistIndex != -1 ? pathArtist.Remove(semicolonArtistIndex) : pathArtist);
            title = semicolonTitleIndex != -1 ? keyTitle.Remove(0, semicolonTitleIndex + 1) : "";
            artist = semicolonArtistIndex != -1 ? pathArtist.Remove(0, semicolonArtistIndex + 1) : "";
        }

        public Song(SaveSong saveSong)
        {
            title = saveSong.Title;
            artist = saveSong.Artist;
            uri = new Uri(saveSong.Path, UriKind.Absolute);
            naturalDurationMilliseconds = saveSong.NaturalDurationMilliseconds;
        }

        private void SetTitelAndArtist()
        {
            title = System.IO.Path.GetFileNameWithoutExtension(Path);
            artist = "Unkown";

            try
            {
                var fileTask = StorageFile.GetFileFromPathAsync(Path);
                fileTask.AsTask().Wait();
                StorageFile file = fileTask.GetResults();

                var fileStreamTask = file.OpenStreamForReadAsync();
                fileStreamTask.Wait();

                Stream fileStream = fileStreamTask.Result;

                TagLib.File tagFile = TagLib.File.Create(new TagLib.StreamFileAbstraction(file.Name, fileStream, fileStream));

                var tags = tagFile.GetTag(TagLib.TagTypes.Id3v2);

                title = tags != null && tags.Title != null && tags.Title != "" ? tags.Title : title;
                artist = tags != null && tags.Artists != null && tags.Artists.Length > 0 ? tags.Artists[0] : "";
            }
            catch (Exception e) { }
        }

        public async Task<StorageFile> GetStorageFile()
        {
            try
            {
                return await StorageFile.GetFileFromPathAsync(Path);
            }
            catch { }

            return null;
        }

        public override string ToString()
        {
            return Artist != "" ? Artist + " - " + Title : Title;
        }
    }
}
