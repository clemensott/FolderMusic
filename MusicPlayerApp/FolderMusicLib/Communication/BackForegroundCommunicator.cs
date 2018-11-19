using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Core;

namespace MusicPlayer.Communication
{
    class BackForegroundCommunicator
    {
        private const string primaryKey = "Primary",
            currentPlaylistPathKey = "CurrentPlaylistPath", currentSongPathKey = "CurrentSongPath",
            songPathKey = "SongPath", playlistPathKey = "PlaylistPath", addKey = "Add", removeKey = "remove",
            artistPrimaryKey = "Artist" + primaryKey,
            titlePrimaryKey = "Title" + primaryKey,
            durationPrimaryKey = "Duration" + primaryKey,
            songPositionPrimaryKey = "SongPosition" + primaryKey,
            currentSongPrimaryKey = "CurrentSong" + primaryKey,
            songsPropertPrimaryKey = "SongsProperty" + primaryKey,
            songsCollectionPrimaryKey = "SongsCollection" + primaryKey,
            shufflePropertyPrimaryKey = "ShuffleProperty" + primaryKey,
            shuffleCollectionPrimaryKey = "ShuffleCollection" + primaryKey,
            loopPrimaryKey = "Loop" + primaryKey,
            libraryPrimaryKey = "Library" + primaryKey,
            playlistsPropertyPrimaryKey = "PlaylistsProperty" + primaryKey,
            playlistsCollectionPrimaryKey = "PlaylistsCollection" + primaryKey,
            currentPlaylistPrimaryKey = "CurrentPlaylist" + primaryKey,
            settingsPrimaryKey = "Settings" + primaryKey,
            playStatePrimaryKey = "PlayState" + primaryKey,
            getLibraryPrimaryKey = "GetLibrary" + primaryKey,
            skipPrimaryKey = "Skip" + primaryKey,

            currentSongPositionKey = "SongPosition",
            shuffleKey = "Shuffle",
            libraryEmptyValue = "LibraryIsEmpty";

        private ILibrary library;
        private List<Tuple<int, ValueSet>> receivingItems;
        private Action<ValueSet> senderMethod;
        private Dictionary<string, Receiver> receivers;

        public BackForegroundCommunicator(ILibrary library)
        {
            receivingItems = new List<Tuple<int, ValueSet>>();
            receivers = GetAllReceiver().ToDictionary(r => r.Key);

            this.library = library;

            if (library.IsForeground)
            {
                BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceived;
                senderMethod = BackgroundMediaPlayer.SendMessageToBackground;

                GetLibrary();
            }
            else
            {
                BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceived;
                senderMethod = BackgroundMediaPlayer.SendMessageToForeground;

                library.SkippedSongs.SkippedSong += OnSkippedSong;
            }

            if (!library.IsLoaded) library.Loaded += OnLibraryLoaded;
            else
            {
                library.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
                library.PlaylistsChanged += OnPlaylistsPropertyChanged;

                Subscribe(library.Playlists);
            }
        }

        private IEnumerable<Receiver> GetAllReceiver()
        {
            yield return new Receiver(titlePrimaryKey, new Action<ValueSet, string>(ReceiveSongTitleChanged));
            yield return new Receiver(artistPrimaryKey, new Action<ValueSet, string>(ReceiveSongArtistChanged));
            yield return new Receiver(durationPrimaryKey, new Action<ValueSet, string>(ReceiveSongDurationChanged));
            yield return new Receiver(currentSongPrimaryKey, new Action<ValueSet, string>(ReceiveCurrentSongChanged));
            yield return new Receiver(songsPropertPrimaryKey, new Action<ValueSet, string>(ReceiveSongsPropertyChanged));
            yield return new Receiver(songsCollectionPrimaryKey, new Action<ValueSet, string>(ReceiveSongsChanged));
            yield return new Receiver(shufflePropertyPrimaryKey, new Action<ValueSet, string>(ReceiveShufflePropertyChanged));
            yield return new Receiver(shuffleCollectionPrimaryKey, new Action<ValueSet, string>(ReceiveShuffleCollectionChanged));
            yield return new Receiver(loopPrimaryKey, new Action<ValueSet, string>(ReceiveLoop));
            yield return new Receiver(libraryPrimaryKey, new Action<ValueSet, string>(ReceiveLibrary));
            yield return new Receiver(playlistsPropertyPrimaryKey, new Action<ValueSet, string>(ReceivePlaylistsPropertyChanged));
            yield return new Receiver(playlistsCollectionPrimaryKey, new Action<ValueSet, string>(ReceivePlaylistsCollectionChanged));
            yield return new Receiver(currentPlaylistPrimaryKey, new Action<ValueSet, string>(ReceiveCurrentPlaylist));
            yield return new Receiver(settingsPrimaryKey, new Action<ValueSet, string>(ReceiveSettings));
            yield return new Receiver(playStatePrimaryKey, new Action<ValueSet, string>(ReceivePlayState));
            yield return new Receiver(getLibraryPrimaryKey, new Action<ValueSet, string>(ReceiveGetLibrary));
            yield return new Receiver(skipPrimaryKey, new Action<ValueSet, string>(ReceiveSkippedSong));
        }

        private void Subscribe(IPlaylistCollection playlists)
        {
            if (playlists == null) return;

            playlists.Changed += OnPlaylistsCollectionChanged;

            Subscribe((IEnumerable<IPlaylist>)playlists);
        }

        private void Unsubscribe(IPlaylistCollection playlists)
        {
            if (playlists == null) return;

            playlists.Changed -= OnPlaylistsCollectionChanged;

            Unsubscribe((IEnumerable<IPlaylist>)playlists);
        }

        private void Subscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>()) Subscribe(playlist);
        }

        private void Unsubscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>()) Unsubscribe(playlist);
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.LoopChanged += OnLoopChanged;
            playlist.SongsChanged += OnSongsPropertyChanged;

            Subscribe(playlist.Songs);
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.SongsChanged -= OnSongsPropertyChanged;

            Unsubscribe(playlist.Songs);
        }

        private void Subscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.Changed += OnSongsCollectionChanged;
            songs.ShuffleChanged += OnShuffleChanged;

            Subscribe(songs.Shuffle);
            Subscribe((IEnumerable<Song>)songs);
        }

        private void Unsubscribe(ISongCollection songs)
        {
            if (songs == null) return;

            songs.Changed -= OnSongsCollectionChanged;
            songs.ShuffleChanged -= OnShuffleChanged;

            Unsubscribe(songs.Shuffle);
            Unsubscribe((IEnumerable<Song>)songs);
        }

        private void Subscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed += OnShuffleCollectionChanged;
        }

        private void Unsubscribe(IShuffleCollection shuffle)
        {
            if (shuffle != null) shuffle.Changed -= OnShuffleCollectionChanged;
        }

        private void Subscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>()) Subscribe(song);
        }

        private void Unsubscribe(IEnumerable<Song> songs)
        {
            foreach (Song song in songs ?? Enumerable.Empty<Song>()) Unsubscribe(song);
        }

        private void Subscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged += OnArtistChanged;
            song.DurationChanged += OnDurationChanged;
            song.TitleChanged += OnTitleChanged;
        }

        private void Unsubscribe(Song song)
        {
            if (song?.IsEmpty ?? true) return;

            song.ArtistChanged -= OnArtistChanged;
            song.DurationChanged -= OnDurationChanged;
            song.TitleChanged -= OnTitleChanged;
        }


        private void OnArtistChanged(object sender, SongArtistChangedEventArgs args)
        {
            Song song = (Song)sender;
            string value = song.Artist;
            string songPath = song.Path;

            ValueSet valueSet = receivers[artistPrimaryKey].GetValueSet(value);
            valueSet.Add(songPathKey, songPath);

            Send(valueSet);
        }

        private void ReceiveSongArtistChanged(ValueSet valueSet, string value)
        {
            string songPath = valueSet[songPathKey].ToString();

            Song changedSong;
            if (!HaveSong(songPath, out changedSong)) return;

            changedSong.Artist = value;
        }


        private void OnTitleChanged(object sender, SongTitleChangedEventArgs args)
        {
            Song song = (Song)sender;
            string value = song.Title;
            string songPath = song.Path;

            ValueSet valueSet = receivers[titlePrimaryKey].GetValueSet(value);
            valueSet.Add(songPathKey, songPath);

            Send(valueSet);
        }

        private void ReceiveSongTitleChanged(ValueSet valueSet, string value)
        {
            string songPath = valueSet[songPathKey].ToString();

            Song changedSong;
            if (!HaveSong(songPath, out changedSong)) return;

            changedSong.Title = value;
        }


        private void OnDurationChanged(object sender, SongDurationChangedEventArgs args)
        {
            Song song = (Song)sender;
            double value = song.DurationMilliseconds;
            string songPath = song.Path;

            ValueSet valueSet = receivers[durationPrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(songPathKey, songPath);

            Send(valueSet);
        }

        private void ReceiveSongDurationChanged(ValueSet valueSet, string value)
        {
            string songPath = valueSet[songPathKey].ToString();

            Song changedSong;
            if (!HaveSong(songPath, out changedSong)) return;

            changedSong.DurationMilliseconds = double.Parse(value);
        }


        public void OnCurrentSongChanged(object sender, CurrentSongChangedEventArgs args)
        {
            string value = ((IPlaylist)sender).CurrentSong.Path;
            ValueSet valueSet = receivers[currentSongPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceiveCurrentSongChanged(ValueSet valueSet, string value)
        {
            Song newCurrentSong;

            if (!HaveSong(value, out newCurrentSong)) return;

            newCurrentSong.Parent.Parent.CurrentSong = newCurrentSong;
        }


        private void OnSongsPropertyChanged(object sender, SongsChangedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)sender;
            string value = XmlConverter.Serialize(playlist.Songs);
            string playlistPath = playlist.AbsolutePath;

            ValueSet valueSet = receivers[songsPropertPrimaryKey].GetValueSet(value);
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        private void ReceiveSongsPropertyChanged(ValueSet valueSet, string value)
        {
            string playlistPath = valueSet[playlistPathKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            changedPlaylist.Songs = XmlConverter.Deserialize(new SongCollection(), value);
        }


        public void OnSongsCollectionChanged(object sender, SongCollectionChangedEventArgs args)
        {
            Unsubscribe(args.GetRemoved());
            Subscribe(args.GetAdded());

            ISongCollection songs = (ISongCollection)sender;
            string removeXml = XmlConverter.Serialize(args.GetRemoved().ToArray());
            string addXml = XmlConverter.Serialize(args.GetAdded().ToArray());
            string playlistPath = songs.Parent.AbsolutePath;

            ValueSet valueSet = receivers[songsCollectionPrimaryKey].GetValueSet(string.Empty);
            valueSet.Add(removeKey, removeXml);
            valueSet.Add(addKey, addXml);
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        private void ReceiveSongsChanged(ValueSet valueSet, string value)
        {
            string playlistPath = valueSet[playlistPathKey].ToString();
            string removeXml = valueSet[removeKey].ToString();
            string addXml = valueSet[addKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            Song[] removes = XmlConverter.Deserialize<Song[]>(removeXml);
            Song[] adds = XmlConverter.Deserialize<Song[]>(addXml);

            changedPlaylist.Songs.Change(removes, adds);
        }


        public void OnShuffleChanged(object sender, ShuffleChangedEventArgs args)
        {
            ISongCollection songs = (ISongCollection)sender;
            string value = Enum.GetName(typeof(ShuffleType), args.NewShuffleType);
            string shuffleXml = XmlConverter.Serialize(args.NewShuffleSongs);
            string playlistPath = songs.Parent.AbsolutePath;

            ValueSet valueSet = receivers[shufflePropertyPrimaryKey].GetValueSet(value);
            valueSet.Add(shuffleKey, shuffleXml);
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        private void ReceiveShufflePropertyChanged(ValueSet valueSet, string value)
        {
            ShuffleType shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType), value);
            string shuffleXml = valueSet[shuffleKey].ToString();
            string playlistPath = valueSet[playlistPathKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            changedPlaylist.Songs.Shuffle = GetShuffleCollection(shuffle, changedPlaylist.Songs, shuffleXml);
        }


        private void OnShuffleCollectionChanged(object sender, ShuffleCollectionChangedEventArgs args)
        {
            IShuffleCollection shuffle = (IShuffleCollection)sender;
            string removeXml = XmlConverter.Serialize(args.GetRemoved().ToArray());
            string addXml = XmlConverter.Serialize(args.AddedSongs);
            string playlistPath = shuffle.Parent.Parent.AbsolutePath;

            ValueSet valueSet = receivers[shuffleCollectionPrimaryKey].GetValueSet(string.Empty);
            valueSet.Add(removeKey, removeXml);
            valueSet.Add(addKey, addXml);
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        public void ReceiveShuffleCollectionChanged(ValueSet valueSet, string value)
        {
            string playlistPath = valueSet[playlistPathKey].ToString();
            string removeXml = valueSet[removeKey].ToString();
            string addXml = valueSet[addKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            Song[] removes = XmlConverter.Deserialize<Song[]>(removeXml);
            ChangeCollectionItem<Song>[] adds = XmlConverter.Deserialize<ChangeCollectionItem<Song>[]>(addXml);

            changedPlaylist.Songs.Shuffle.Change(removes, adds);
        }


        public void OnLoopChanged(object sender, LoopChangedEventArgs args)
        {
            IPlaylist playlist = (IPlaylist)sender;
            string value = Enum.GetName(typeof(LoopType), playlist.Loop);
            string playlistPath = playlist.AbsolutePath;

            ValueSet valueSet = receivers[loopPrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        private void ReceiveLoop(ValueSet valueSet, string value)
        {
            LoopType loop = (LoopType)Enum.Parse(typeof(LoopType), value);
            string playlistPath = valueSet[playlistPathKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            changedPlaylist.Loop = loop;
        }

        private void OnPlaylistsPropertyChanged(object sender, PlaylistsChangedEventArgs args)
        {
            Unsubscribe(args.OldPlaylists);
            Subscribe(args.NewPlaylists);

            string value = XmlConverter.Serialize(library.Playlists);
            ValueSet valueSet = receivers[playlistsPropertyPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceivePlaylistsPropertyChanged(ValueSet valueSet, string value)
        {
            library.Playlists = XmlConverter.DeserializeNew<PlaylistCollection>(value);
        }


        private void OnPlaylistsCollectionChanged(object sender, PlaylistCollectionChangedEventArgs args)
        {
            Unsubscribe(args.GetRemoved());
            Subscribe(args.GetAdded());

            string removeXml = XmlConverter.Serialize(args.GetRemoved().ToArray());
            string addXml = XmlConverter.Serialize(args.GetAdded().ToArray());

            ValueSet valueSet = receivers[playlistsPropertyPrimaryKey].GetValueSet(string.Empty);
            valueSet.Add(removeKey, removeXml);
            valueSet.Add(addKey, addXml);

            Send(valueSet);
        }

        private void ReceivePlaylistsCollectionChanged(ValueSet valueSet, string value)
        {
            string removeXml = valueSet[removeKey].ToString();
            string addXml = valueSet[addKey].ToString();

            IPlaylist[] removes = XmlConverter.Deserialize<IPlaylist[]>(removeXml);
            IPlaylist[] adds = XmlConverter.Deserialize<IPlaylist[]>(addXml);

            library.Playlists.Change(removes, adds);
        }


        public void OnCurrentPlaylistChanged(object sender, CurrentPlaylistChangedEventArgs args)
        {
            string value = args.NewCurrentPlaylist.AbsolutePath;
            ValueSet valueSet = receivers[currentPlaylistPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceiveCurrentPlaylist(ValueSet valueSet, string value)
        {
            IPlaylist newCurrentPlaylist;
            if (!HavePlaylist(value, out newCurrentPlaylist)) return;

            library.CurrentPlaylist = newCurrentPlaylist;
        }


        public void OnPlayStateChanged(object sender, PlayStateChangedEventArgs args)
        {
            string value = args.NewValue.ToString();
            ValueSet valueSet = receivers[playStatePrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceivePlayState(ValueSet valueSet, string value)
        {
            library.IsPlaying = bool.Parse(value);
        }


        private void OnLibraryLoaded(object sender, EventArgs args)
        {
            library.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            library.PlaylistsChanged += OnPlaylistsPropertyChanged;
            library.PlayStateChanged += OnPlayStateChanged;
            library.Loaded -= OnLibraryLoaded;

            Unsubscribe(library.Playlists);
            Subscribe(library.Playlists);

            if (!library.IsForeground) SendLibrary();

            OnPlayStateChanged(library, new PlayStateChangedEventArgs(library.IsPlaying));
        }


        public void OnSettingsChanged()
        {
            string value = string.Empty;
            ValueSet valueSet = receivers[settingsPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        public void GetLibrary()
        {
            string value = string.Empty;
            ValueSet valueSet = receivers[getLibraryPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceiveGetLibrary(ValueSet valueSet, string value)
        {
            if (library.IsForeground || !library.IsLoaded) return;

            SendLibrary();
        }

        private void SendLibrary()
        {
            string value = library.Playlists.Count > 0 ? XmlConverter.Serialize(library) : libraryEmptyValue;
            ValueSet valueSet = receivers[libraryPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceiveLibrary(ValueSet valueSet, string value)
        {

            ILibrary receivedLibrary = new Library(library.IsForeground);

            if (value != libraryEmptyValue) receivedLibrary.ReadXml(XmlConverter.GetReader(value));

            library.Load(receivedLibrary.Playlists);
        }


        private void OnSkippedSongsChanged(SkipSongs sender)
        {
            string value = string.Empty;
            ValueSet valueSet = receivers[skipPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }


        private void OnSkippedSong(SkipSongs sender)
        {
            ValueSet valueSet = receivers[skipPrimaryKey].GetValueSet(string.Empty);

            Send(valueSet);
        }

        private void ReceiveSkippedSong(ValueSet valueSet, string value)
        {
            library.SkippedSongs.Raise();
        }


        private void Send(ValueSet valueSet)
        {
            bool send = AllowedToSend(valueSet);
            MobileDebug.Service.WriteEvent("Send", GetPrimaryKey(valueSet), send);
            if (!send) return;

            senderMethod(valueSet);
        }

        private bool AllowedToSend(ValueSet valueSet)
        {
            foreach (var receivingItem in receivingItems.Where(f => f.Item1 == Environment.CurrentManagedThreadId))
            {
                if (Same(valueSet, receivingItem.Item2)) return false;
            }

            return true;
        }

        private bool Same(ValueSet valueSet1, ValueSet valueSet2)
        {
            if (valueSet1.Count != valueSet2.Count) return false;

            foreach (string key in valueSet1.Keys)
            {
                if (!valueSet2.Keys.Contains(key)) return false;
            }

            return true;
        }

        private async void BackgroundMediaPlayer_MessageReceived(object sender, MediaPlayerDataReceivedEventArgs e)
        {

            try
            {
                if (!NeedsDispatcher()) Handle(e.Data);
                else
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.
                         RunAsync(CoreDispatcherPriority.Normal, () => { Handle(e.Data); });
                }
            }
            catch (Exception exc1)
            {
                string currentReceivedPrimaryKey = GetPrimaryKey(e.Data);
                string primaryData = e.Data[currentReceivedPrimaryKey].ToString();
                MobileDebug.Service.WriteEvent("ReceiveFail1", exc1, currentReceivedPrimaryKey);
            }
        }

        private void Handle(ValueSet valueSet)
        {
            var receivingItem = new Tuple<int, ValueSet>(Environment.CurrentManagedThreadId, valueSet);
            receivingItems.Add(receivingItem);

            string currentReceivedPrimaryKey = GetPrimaryKey(valueSet);
            receivers[currentReceivedPrimaryKey].Handle(valueSet);

            receivingItems.Remove(receivingItem);
        }

        private string GetPrimaryKey(ValueSet valueSet)
        {
            return valueSet.Keys.FirstOrDefault(k => k.EndsWith(primaryKey));
        }

        private bool NeedsDispatcher()
        {
            try
            {
                if (!library.IsForeground) return false;

                return !CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess;
            }
            catch
            {
                return false;
            }
        }


        private void ReceiveSettings(ValueSet valueSet, string value)
        {
            //Feedback.Current.RaiseSettingsPropertyChanged();
        }

        private bool HaveSong(string path, out Song song)
        {
            foreach (IPlaylist playlist in library.Playlists)
            {
                foreach (Song s in playlist.Songs)
                {
                    if (s.Path != path) continue;

                    song = s;
                    return true;
                }
            }

            song = null;
            return false;
        }

        private bool HavePlaylist(string path, out IPlaylist playlist)
        {
            foreach (IPlaylist p in library.Playlists)
            {
                if (p.AbsolutePath != path) continue;

                playlist = p;
                return true;
            }

            playlist = null;
            return false;
        }

        private IShuffleCollection GetShuffleCollection(ShuffleType type, ISongCollection songs, string xmlText)
        {
            IShuffleCollection shuffle;

            switch (type)
            {
                case ShuffleType.Off:
                    shuffle = new ShuffleOffCollection(songs);
                    break;

                case ShuffleType.OneTime:
                    shuffle = new ShuffleOneTimeCollection(songs);
                    break;

                case ShuffleType.Complete:
                    shuffle = new ShuffleCompleteCollection(songs);
                    break;

                default:
                    MobileDebug.Service.WriteEvent("Com.GetShuffleCollection", type);
                    throw new NotImplementedException();
            }

            shuffle.ReadXml(XmlConverter.GetReader(xmlText));

            return shuffle;
        }
    }
}
