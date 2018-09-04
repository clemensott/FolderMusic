using MusicPlayer.Data;
using MusicPlayer.Data.Loop;
using MusicPlayer.Data.NonLoaded;
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
            songPathKey = "SongPath", playlistPathKey = "PlaylistPath",
            artistPrimaryKey = "Artist" + primaryKey,
            titlePrimaryKey = "Title" + primaryKey,
            durationPrimaryKey = "Duration" + primaryKey,
            songPositionPrimaryKey = "SongPosition" + primaryKey,
            currentSongPrimaryKey = "CurrentSong" + primaryKey,
            songsPrimaryKey = "Songs" + primaryKey,
            shufflePrimaryKey = "Shuffle" + primaryKey,
            shuffleSongsPrimaryKey = "ShuffleSongs" + primaryKey,
            loopPrimaryKey = "Loop" + primaryKey,
            libraryPrimaryKey = "Library" + primaryKey,
            playlistsPrimaryKey = "Playlists" + primaryKey,
            removePlaylistPrimaryKey = "removePlaylist" + primaryKey,
            currentPlaylistPrimaryKey = "CurrentPlaylist" + primaryKey,
            settingsPrimaryKey = "Settings" + primaryKey,
            playStatePrimaryKey = "PlayState" + primaryKey,
            getLibraryPrimaryKey = "GetLibrary" + primaryKey,
            skipPrimaryKey = "Skip" + primaryKey,

            currentSongPositionKey = "SongPosition",
            shuffleKey = "Shuffle",
            shuffleSongsKey = "ShuffleSongs",
            libraryEmptyValue = "libraryEmpty";

        private static BackForegroundCommunicator instance;

        public static void StartCommunication(ILibrary library, bool isForeground)
        {
            instance = new BackForegroundCommunicator(library, isForeground);
        }

        private bool isForeground;
        private ILibrary library;
        private List<Tuple<int, ValueSet>> receivingItems;
        private Action<ValueSet> senderMethod;
        private Dictionary<string, Receiver> receivers;

        private BackForegroundCommunicator(ILibrary library, bool isForeground)
        {
            receivingItems = new List<Tuple<int, ValueSet>>();
            receivers = GetAllReceiver().ToDictionary(r => r.Key);

            this.library = library;
            this.isForeground = isForeground;

            if (isForeground)
            {
                BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceived;
                senderMethod = BackgroundMediaPlayer.SendMessageToBackground;

                GetLibrary();
            }
            else
            {
                BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceived;
                senderMethod = BackgroundMediaPlayer.SendMessageToForeground;
            }

            library.LibraryChanged += OnLibraryChanged;
            library.PlayStateChanged += OnPlayStateChanged;
            library.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            library.PlaylistsChanged += OnPlaylistsChanged;
            library.SettingsChanged += OnSettingsChanged;
        }

        private IEnumerable<Receiver> GetAllReceiver()
        {
            yield return new Receiver(titlePrimaryKey, new Action<ValueSet, string>(ReceiveSongTitleChanged));
            yield return new Receiver(artistPrimaryKey, new Action<ValueSet, string>(ReceiveSongArtistChanged));
            yield return new Receiver(durationPrimaryKey, new Action<ValueSet, string>(ReceiveSongDurationChanged));
            yield return new Receiver(currentSongPrimaryKey, new Action<ValueSet, string>(ReceiveCurrentSongChanged));
            yield return new Receiver(songsPrimaryKey, new Action<ValueSet, string>(ReceiveSongsChanged));
            yield return new Receiver(shuffleSongsPrimaryKey, new Action<ValueSet, string>(ReceiveShuffleSongsChanged));
            yield return new Receiver(shufflePrimaryKey, new Action<ValueSet, string>(ReceiveShuffleChanged));
            yield return new Receiver(loopPrimaryKey, new Action<ValueSet, string>(ReceiveLoop));
            yield return new Receiver(libraryPrimaryKey, new Action<ValueSet, string>(ReceiveLibrary));
            yield return new Receiver(playlistsPrimaryKey, new Action<ValueSet, string>(ReceivePlaylistsChanged));
            yield return new Receiver(currentPlaylistPrimaryKey, new Action<ValueSet, string>(ReceiveCurrentPlaylist));
            yield return new Receiver(settingsPrimaryKey, new Action<ValueSet, string>(ReceiveSettings));
            yield return new Receiver(playStatePrimaryKey, new Action<ValueSet, string>(ReceivePlayState));
            yield return new Receiver(getLibraryPrimaryKey, new Action<ValueSet, string>(ReceiveGetLibrary));
            yield return new Receiver(skipPrimaryKey, new Action<ValueSet, string>(ReceiveSkip));
        }

        private void Subscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                Subscribe(playlist);
            }
        }

        private void Unsubscribe(IEnumerable<IPlaylist> playlists)
        {
            foreach (IPlaylist playlist in playlists ?? Enumerable.Empty<IPlaylist>())
            {
                Unsubscribe(playlist);
            }
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnCurrentSongChanged;
            playlist.LoopChanged += OnLoopChanged;
            playlist.ShuffleChanged += OnShuffleChanged;

            playlist.Songs.CollectionChanged += OnSongsChanged;
            playlist.ShuffleSongs.Changed += OnShuffleSongsChanged;
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnCurrentSongChanged;
            playlist.LoopChanged -= OnLoopChanged;
            playlist.ShuffleChanged -= OnShuffleChanged;

            playlist.Songs.CollectionChanged -= OnSongsChanged;
            playlist.ShuffleSongs.Changed -= OnShuffleSongsChanged;
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


        private void OnArtistChanged(Song sender, SongArtistChangedEventArgs args)
        {
            string value = sender.Artist;
            string songPath = sender.Path;

            ValueSet valueSet = receivers[artistPrimaryKey].GetValueSet(value);
            valueSet.Add(songPathKey, songPath);

            Send(valueSet);
        }

        private void ReceiveSongArtistChanged(ValueSet valueSet, string value)
        {
            string playlistPath = valueSet[playlistPathKey].ToString();
            string songPath = valueSet[songPathKey].ToString();

            Song changedSong;
            if (!HaveSong(songPath, out changedSong)) return;

            changedSong.Artist = value;
        }


        private void OnTitleChanged(Song sender, SongTitleChangedEventArgs args)
        {
            string value = sender.Title;
            string songPath = sender.Path;

            ValueSet valueSet = receivers[titlePrimaryKey].GetValueSet(value);
            valueSet.Add(songPathKey, songPath);

            Send(valueSet);
        }

        private void ReceiveSongTitleChanged(ValueSet valueSet, string value)
        {
            string playlistPath = valueSet[playlistPathKey].ToString();
            string songPath = valueSet[songPathKey].ToString();

            Song changedSong;
            if (!HaveSong(songPath, out changedSong)) return;

            changedSong.Title = value;
        }


        private void OnDurationChanged(Song sender, SongDurationChangedEventArgs args)
        {
            double value = sender.DurationMilliseconds;
            string songPath = sender.Path;

            ValueSet valueSet = receivers[durationPrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(songPathKey, songPath);

            Send(valueSet);
        }

        private void ReceiveSongDurationChanged(ValueSet valueSet, string value)
        {
            string playlistPath = valueSet[playlistPathKey].ToString();
            string songPath = valueSet[songPathKey].ToString();

            Song changedSong;
            if (!HaveSong(songPath, out changedSong)) return;

            changedSong.DurationMilliseconds = double.Parse(value);
        }


        public void OnCurrentSongChanged(IPlaylist sender, CurrentSongChangedEventArgs args)
        {
            string value = sender.CurrentSong.Path;
            ValueSet valueSet = receivers[currentSongPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceiveCurrentSongChanged(ValueSet valueSet, string value)
        {
            Song newCurrentSong;
            if (!HaveSong(value, out newCurrentSong)) return;

            newCurrentSong.Parent.Parent.CurrentSong = newCurrentSong;
        }


        public void OnSongsChanged(ISongCollection sender, SongCollectionChangedEventArgs args)
        {
            Unsubscribe(args.GetRemoved());
            Subscribe(args.GetAdded());

            string value = XmlConverter.Serialize(sender);
            string shuffleSongs = XmlConverter.Serialize(sender.Parent.ShuffleSongs);
            string currentSongPath = sender.Parent.CurrentSong.Path;
            string position = sender.Parent.CurrentSongPositionPercent.ToString();
            string playlistPath = sender.Parent.AbsolutePath;

            ValueSet valueSet = receivers[songsPrimaryKey].GetValueSet(value);
            valueSet.Add(shuffleSongsKey, shuffleSongs);
            valueSet.Add(currentSongPathKey, currentSongPath);
            valueSet.Add(currentSongPositionKey, position.ToString());
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        private void ReceiveSongsChanged(ValueSet valueSet, string value)
        {
            string shuffleSongsXml = valueSet[shuffleSongsKey].ToString();
            string currentSongPath = valueSet[currentSongPathKey].ToString();
            double position = double.Parse(valueSet[currentSongPositionKey].ToString());
            string playlistPath = valueSet[playlistPathKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            ISongCollection songs = new SongCollection(changedPlaylist, value);
            var shuffleSongs = GetShuffleCollection(changedPlaylist.Shuffle, changedPlaylist, songs, shuffleSongsXml);

            Unsubscribe(changedPlaylist.Songs);
            Subscribe(songs);

            changedPlaylist.Songs.Reset(songs);
            changedPlaylist.ShuffleSongs.Reset(shuffleSongs);

            Song newCurrentSong;
            if (!HaveSong(currentSongPath, out newCurrentSong)) return;

            changedPlaylist.CurrentSong = newCurrentSong;
            changedPlaylist.CurrentSongPositionPercent = position;
        }


        private void OnShuffleSongsChanged(IShuffleCollection sender)
        {
            string value = XmlConverter.Serialize(sender);
            string currentSongPath = sender.Parent.CurrentSong.Path;
            string position = sender.Parent.CurrentSongPositionPercent.ToString();
            string playlistPath = sender.Parent.AbsolutePath;

            ValueSet valueSet = receivers[shuffleSongsPrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(currentSongPathKey, currentSongPath);
            valueSet.Add(currentSongPositionKey, position);
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        public void ReceiveShuffleSongsChanged(ValueSet valueSet, string value)
        {
            string currentSongPath = valueSet[currentSongPathKey].ToString();
            double position = double.Parse(valueSet[currentSongPositionKey].ToString());
            string playlistPath = valueSet[playlistPathKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            var shuffleSongs = GetShuffleCollection(changedPlaylist.Shuffle, changedPlaylist, changedPlaylist.Songs, value);

            changedPlaylist.ShuffleSongs.Reset(shuffleSongs);

            Song newCurrentSong;
            if (!HaveSong(currentSongPath, out newCurrentSong)) return;

            changedPlaylist.CurrentSong = newCurrentSong;
            changedPlaylist.CurrentSongPositionPercent = position;
        }


        public void OnShuffleChanged(IPlaylist sender, ShuffleChangedEventArgs args)
        {
            string value = sender.Shuffle.ToString();
            string shuffleSongs = XmlConverter.Serialize(sender.ShuffleSongs);
            string currentSongPath = sender.CurrentSong.Path;
            string position = sender.CurrentSongPositionPercent.ToString();
            string playlistPath = sender.AbsolutePath;

            ValueSet valueSet = receivers[shufflePrimaryKey].GetValueSet(value);
            valueSet.Add(shuffleSongsKey, shuffleSongs);
            valueSet.Add(currentSongPathKey, currentSongPath);
            valueSet.Add(currentSongPositionKey, position);
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        private void ReceiveShuffleChanged(ValueSet valueSet, string value)
        {
            ShuffleType shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType), value);
            string shuffleSongsXml = valueSet[shuffleSongsKey].ToString();
            string currentSongPath = valueSet[currentSongPathKey].ToString();
            double position = double.Parse(valueSet[currentSongPositionKey].ToString());
            string playlistPath = valueSet[playlistPathKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            var shuffleSongs = GetShuffleCollection(shuffle, changedPlaylist, changedPlaylist.Songs, shuffleSongsXml);

            changedPlaylist.SetShuffle(shuffleSongs);

            Song newCurrentSong;
            if (!HaveSong(currentSongPath, out newCurrentSong)) return;

            changedPlaylist.CurrentSong = newCurrentSong;
            changedPlaylist.CurrentSongPositionPercent = position;
        }


        public void OnLoopChanged(IPlaylist sender, LoopChangedEventArgs args)
        {
            string value = sender.Loop.ToString();
            string playlistPath = sender.AbsolutePath;

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


        private void OnPlaylistsChanged(ILibrary sender, PlaylistsChangedEventArgs args)
        {
            Unsubscribe(args.GetRemoved());
            Subscribe(args.GetAdded());

            string value = XmlConverter.Serialize(sender.Playlists);
            string currentPlaylistPath = sender.CurrentPlaylist.AbsolutePath;

            ValueSet valueSet = receivers[playlistsPrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(currentPlaylistPathKey, currentPlaylistPath);

            Send(valueSet);
        }

        private void ReceivePlaylistsChanged(ValueSet valueSet, string value)
        {
            IPlaylistCollection playlists = new PlaylistCollection(library, value);
            string currentPlaylistPath = valueSet[currentPlaylistPathKey].ToString();

            library.Playlists.Change(playlists, library.Playlists);

            IPlaylist newCurrentPlaylist;
            if (!HavePlaylist(currentPlaylistPath, out newCurrentPlaylist)) return;

            library.CurrentPlaylist = newCurrentPlaylist;
            library.Save();
        }


        public void OnCurrentPlaylistChanged(ILibrary sender, CurrentPlaylistChangedEventArgs args)
        {
            string value = sender.CurrentPlaylist.AbsolutePath;
            ValueSet valueSet = receivers[currentPlaylistPrimaryKey].GetValueSet(value.ToString());

            Send(valueSet);
        }

        private void ReceiveCurrentPlaylist(ValueSet valueSet, string value)
        {
            IPlaylist newCurrentPlaylist;
            if (!HavePlaylist(value, out newCurrentPlaylist)) return;

            library.CurrentPlaylist = newCurrentPlaylist;
        }


        public void OnPlayStateChanged(ILibrary sender, PlayStateChangedEventArgs args)
        {
            string value = args.NewValue.ToString();
            ValueSet valueSet = receivers[playStatePrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceivePlayState(ValueSet valueSet, string value)
        {
            library.IsPlaying = bool.Parse(value);
        }


        private void OnLibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            Unsubscribe(args.OldPlaylists);
            Subscribe(args.NewPlaylists);

            SendLibrary();
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
            if (isForeground || library.CurrentPlaylist is NonLoadedPlaylist || library.Playlists.Count == 0) return;

            SendLibrary();
        }

        private void SendLibrary()
        {
            string value = XmlConverter.Serialize(library);
            ValueSet valueSet = receivers[libraryPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceiveLibrary(ValueSet valueSet, string value)
        {
            ILibrary receivedLibrary = new Library(value);
            MobileDebug.Manager.WriteEvent("ReceiveLibrary", receivedLibrary.Playlists.Count);
            library.Set(receivedLibrary);
        }


        private void OnSkippedSongsChanged(SkipSongs sender)
        {
            string value = string.Empty;
            ValueSet valueSet = receivers[skipPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }


        private void Send(ValueSet valueSet)
        {
            bool send = AllowedToSend(valueSet);
            MobileDebug.Manager.WriteEvent("Send", GetPrimaryKey(valueSet), send);
            if (!send) return;

            MediaPlayer player = BackgroundMediaPlayer.Current;     // Player abrufen zum starten
            senderMethod(valueSet);
        }

        private bool AllowedToSend(ValueSet valueSet)
        {
            var receivingItem = receivingItems.FirstOrDefault(f => f.Item1 == Environment.CurrentManagedThreadId);

            if (receivingItem == null) return true;

            return !Same(valueSet, receivingItem.Item2);
        }

        private bool Same(ValueSet valueSet1, ValueSet valueSet2)
        {
            if (valueSet1.Count != valueSet2.Count) return false;

            for (int i = 0; i < valueSet1.Count; i++)
            {
                if (valueSet1.ElementAt(i).Key != valueSet2.ElementAt(i).Key) return false;
            }

            return true;
        }

        private void BackgroundMediaPlayer_MessageReceived(object sender, MediaPlayerDataReceivedEventArgs e)
        {

            try
            {
                if (!UseDispatcher()) Handle(e.Data);
                else
                {
                    CoreApplication.MainView.CoreWindow.Dispatcher.
                        RunAsync(CoreDispatcherPriority.Normal, () => { Handle(e.Data); });
                }
            }
            catch (Exception exc1)
            {
                string currentReceivedPrimaryKey = GetPrimaryKey(e.Data);
                string primaryData = e.Data[currentReceivedPrimaryKey].ToString();
                MobileDebug.Manager.WriteEvent("ReceiveFail1", exc1, currentReceivedPrimaryKey);
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

        private bool UseDispatcher()
        {
            try
            {
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

        private void ReceiveSkip(ValueSet valueSet, string value)
        {
            //if (isForeground) Feedback.Current.RaiseSkippedSongsPropertyChanged();
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

        private IShuffleCollection GetShuffleCollection(ShuffleType shuffle, IPlaylist playlist, ISongCollection songs, string xmlText)
        {
            switch (shuffle)
            {
                case ShuffleType.Off:
                    return new ShuffleOffCollection(playlist, songs, xmlText);

                case ShuffleType.OneTime:
                    return new ShuffleOneTimeCollection(playlist, songs, xmlText);

                case ShuffleType.Complete:
                    return new ShuffleCompleteCollection(playlist, songs, xmlText);
            }

            return null;
        }
    }
}
