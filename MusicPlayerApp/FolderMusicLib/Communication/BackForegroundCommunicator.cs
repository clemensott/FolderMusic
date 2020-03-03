using MusicPlayer.Data;
using MusicPlayer.Data.Shuffle;
using MusicPlayer.Data.SubscriptionsHandler;
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
            currentSongPrimaryKey = "CurrentSongFileName" + primaryKey,
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
            isPlayingPrimaryKey = "IsPlaying" + primaryKey,
            playerStatePrimaryKey = "PlayerState" + primaryKey,
            getLibraryPrimaryKey = "GetLibrary" + primaryKey,
            skipPrimaryKey = "Skip" + primaryKey,

            currentSongPositionKey = "SongPosition",
            shuffleKey = "Shuffle",
            libraryEmptyValue = "LibraryIsEmpty";

        private readonly List<Tuple<int, ValueSet>> receivingItems;
        private readonly Dictionary<string, Receiver> receivers;
        private readonly LibrarySubscriptionsHandler lsh;
        private readonly ILibrary library;
        private readonly Action<ValueSet> senderMethod;

        public BackForegroundCommunicator(ILibrary library)
        {
            receivingItems = new List<Tuple<int, ValueSet>>();
            receivers = GetAllReceiver().ToDictionary(r => r.Key);
            lsh = LibrarySubscriptionsHandler.GetInstance(library);

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

                lsh.SkippedSong += OnSkippedSong;
            }

            lsh.Loaded += OnLoaded;
            lsh.CurrentPlaylistChanged += OnCurrentPlaylistChanged;
            lsh.PlaylistsPropertyChanged += OnPlaylistsPropertyChanged;
            lsh.PlaylistCollectionChanged += OnPlaylistCollectionChanged;
            lsh.IsPlayingChanged += OnIsPlayingChanged;
            lsh.PlayerStateChanged += OnPlayerStateChanged;
            lsh.AllPlaylists.CurrentSongChanged += OnAllPlaylists_CurrentSongChanged;
            lsh.AllPlaylists.CurrentSongPositionChanged += OnAllPlaylists_CurrentSongPositionChanged;
            lsh.AllPlaylists.LoopChanged += OnAllPlaylists_LoopChanged;
            lsh.AllPlaylists.SongsPropertyChanged += OnAllPlaylists_SongsPropertyChanged;
            lsh.AllPlaylists.SongCollectionChanged += OnAllPlaylists_SongCollectionChanged;
            lsh.AllPlaylists.ShuffleChanged += OnAllPlaylists_ShuffleChanged;
            lsh.AllPlaylists.ShuffleCollectionChanged += OnAllPlaylists_ShuffleCollectionChanged;
            lsh.AllPlaylists.AllSongs.ArtistChanged += OnAllPlaylists_AllSongs_ArtistChanged;
            lsh.AllPlaylists.AllSongs.TitleChanged += OnAllPlaylist_AllSongs_TitleChanged;
            lsh.AllPlaylists.AllSongs.DurationChanged += OnAllPlaylists_AllSongs_DurationChanged;
        }

        private IEnumerable<Receiver> GetAllReceiver()
        {
            yield return new Receiver(titlePrimaryKey, ReceiveSongTitleChanged);
            yield return new Receiver(artistPrimaryKey, ReceiveSongArtistChanged);
            yield return new Receiver(durationPrimaryKey, ReceiveSongDurationChanged);
            yield return new Receiver(currentSongPrimaryKey, ReceiveCurrentSongChanged);
            yield return new Receiver(songPositionPrimaryKey, ReceiveSongPositionChanged);
            yield return new Receiver(songsPropertPrimaryKey, ReceiveSongsPropertyChanged);
            yield return new Receiver(songsCollectionPrimaryKey, ReceiveSongsChanged);
            yield return new Receiver(shufflePropertyPrimaryKey, ReceiveShufflePropertyChanged);
            yield return new Receiver(shuffleCollectionPrimaryKey, ReceiveShuffleCollectionChanged);
            yield return new Receiver(loopPrimaryKey, ReceiveLoop);
            yield return new Receiver(libraryPrimaryKey, ReceiveLibrary);
            yield return new Receiver(playlistsPropertyPrimaryKey, ReceivePlaylistsPropertyChanged);
            yield return new Receiver(playlistsCollectionPrimaryKey, ReceivePlaylistsCollectionChanged);
            yield return new Receiver(currentPlaylistPrimaryKey, ReceiveCurrentPlaylist);
            yield return new Receiver(settingsPrimaryKey, ReceiveSettings);
            yield return new Receiver(isPlayingPrimaryKey, ReceivePlayState);
            yield return new Receiver(playerStatePrimaryKey, ReceivePlayerState);
            yield return new Receiver(getLibraryPrimaryKey, ReceiveGetLibrary);
            yield return new Receiver(skipPrimaryKey, ReceiveSkippedSong);
        }

        private void OnAllPlaylists_AllSongs_ArtistChanged(object sender, SubscriptionsEventArgs<Song, SongArtistChangedEventArgs> e)
        {
            string value = e.Source.Artist;
            string songPath = e.Source.Path;

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


        private void OnAllPlaylist_AllSongs_TitleChanged(object sender, SubscriptionsEventArgs<Song, SongTitleChangedEventArgs> e)
        {
            string value = e.Source.Title;
            string songPath = e.Source.Path;

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


        private void OnAllPlaylists_AllSongs_DurationChanged(object sender, SubscriptionsEventArgs<Song, SongDurationChangedEventArgs> e)
        {
            double value = e.Source.DurationMilliseconds;
            string songPath = e.Source.Path;

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


        private void OnAllPlaylists_CurrentSongChanged(object sender, SubscriptionsEventArgs<IPlaylist, CurrentSongChangedEventArgs> e)
        {
            string value = e.Source.CurrentSong.Path;
            ValueSet valueSet = receivers[currentSongPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceiveCurrentSongChanged(ValueSet valueSet, string value)
        {
            Song newCurrentSong;

            if (!HaveSong(value, out newCurrentSong)) return;

            newCurrentSong.Parent.Parent.CurrentSong = newCurrentSong;
        }


        private void OnAllPlaylists_CurrentSongPositionChanged(object sender, SubscriptionsEventArgs<IPlaylist, CurrentSongPositionChangedEventArgs> e)
        {
            string value = e.Base.NewCurrentSongPosition.ToString();
            string playlistPath = e.Source.AbsolutePath;

            ValueSet valueSet = receivers[songPositionPrimaryKey].GetValueSet(value);
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        private void ReceiveSongPositionChanged(ValueSet valueSet, string value)
        {
            string playlistPath = valueSet[playlistPathKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            changedPlaylist.CurrentSongPosition = double.Parse(value);
        }


        private void OnAllPlaylists_SongsPropertyChanged(object sender, SubscriptionsEventArgs<IPlaylist, SongsChangedEventArgs> e)
        {
            string value = XmlConverter.Serialize(e.Source.Songs);
            string playlistPath = e.Source.AbsolutePath;

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


        private void OnAllPlaylists_SongCollectionChanged(object sender, SubscriptionsEventArgs<ISongCollection, SongCollectionChangedEventArgs> e)
        {
            string removeXml = XmlConverter.Serialize(e.Base.GetRemoved().ToArray());
            string addXml = XmlConverter.Serialize(e.Base.GetAdded().ToArray());
            string playlistPath = e.Source.Parent.AbsolutePath;

            ValueSet valueSet = receivers[songsCollectionPrimaryKey].GetValueSet(string.Empty);
            valueSet.Add(removeKey, removeXml);
            valueSet.Add(addKey, addXml);
            valueSet.Add(playlistPathKey, playlistPath);

            Send(valueSet);
        }

        private void ReceiveSongsChanged(ValueSet valueSet, string value)
        {
            MobileDebug.Service.WriteEvent("ReceiveSongs1");
            string playlistPath = valueSet[playlistPathKey].ToString();
            string removeXml = valueSet[removeKey].ToString();
            string addXml = valueSet[addKey].ToString();

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            Song[] removes = XmlConverter.Deserialize<Song[]>(removeXml);
            Song[] adds = XmlConverter.Deserialize<Song[]>(addXml);
            MobileDebug.Service.WriteEvent("ReceiveSongs2", adds.Length, removes.Length);
            for (int i = 0; i < removes.Length; i++)
            {
                Song song = removes[i];
                song = changedPlaylist.Songs.FirstOrDefault(s => s.Path == song.Path);

                if (song != null) removes[i] = song;
            }

            for (int i = 0; i < adds.Length; i++)
            {
                Song song = adds[i];
                song = changedPlaylist.Songs.Shuffle.FirstOrDefault(s => s.Path == song.Path);

                if (song != null) adds[i] = song;
            }
            MobileDebug.Service.WriteEvent("ReceiveSongs3");
            changedPlaylist.Songs.Change(removes, adds);
        }


        private void OnAllPlaylists_ShuffleChanged(object sender, SubscriptionsEventArgs<ISongCollection, ShuffleChangedEventArgs> e)
        {
            string value = Enum.GetName(typeof(ShuffleType), e.Base.NewShuffleType);
            string shuffleXml = XmlConverter.Serialize(e.Base.NewShuffleSongs);
            string playlistPath = e.Source.Parent.AbsolutePath;

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


        private void OnAllPlaylists_ShuffleCollectionChanged(object sender, SubscriptionsEventArgs<IShuffleCollection, ShuffleCollectionChangedEventArgs> e)
        {
            string removeXml = XmlConverter.Serialize(e.Base.GetRemoved().ToArray());
            string addXml = XmlConverter.Serialize(e.Base.AddedSongs);
            string playlistPath = e.Source.Parent.Parent.AbsolutePath;

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

            //SaveText(removeXml);

            IPlaylist changedPlaylist;
            if (!HavePlaylist(playlistPath, out changedPlaylist)) return;

            Song[] removes = XmlConverter.Deserialize<Song[]>(removeXml);
            ChangeCollectionItem<Song>[] adds = XmlConverter.Deserialize<ChangeCollectionItem<Song>[]>(addXml);

            for (int i = 0; i < removes.Length; i++)
            {
                Song song = removes[i];
                song = changedPlaylist.Songs.Shuffle.FirstOrDefault(s => s.Path == song.Path);

                if (song != null) removes[i] = song;
            }

            for (int i = 0; i < adds.Length; i++)
            {
                Song song = adds[i].Item;
                song = changedPlaylist.Songs.FirstOrDefault(s => s.Path == song.Path);

                if (song != null) adds[i].Item = song;
            }

            changedPlaylist.Songs.Shuffle.Change(removes, adds);
        }

        //private async void SaveText(string text)
        //{
        //    MobileDebug.Service.WriteEvent("Com.SaveText1");
        //    StorageFile file;

        //    try
        //    {
        //        file = await KnownFolders.VideosLibrary.GetFileAsync("text.txt");
        //    }
        //    catch (Exception e1)
        //    {
        //        MobileDebug.Service.WriteEvent("Com.SaveTextFail1", e1);

        //        try
        //        {
        //            file = await KnownFolders.VideosLibrary.CreateFileAsync("text.txt");
        //        }
        //        catch (Exception e2)
        //        {
        //            MobileDebug.Service.WriteEvent("Com.SaveTextFail2", e2);
        //            return;
        //        }
        //    }

        //    MobileDebug.Service.WriteEvent("Com.SaveText2", file.Name);
        //    await FileIO.WriteTextAsync(file, text);
        //    MobileDebug.Service.WriteEvent("Com.SaveText3");
        //}


        private void OnAllPlaylists_LoopChanged(object sender, SubscriptionsEventArgs<IPlaylist, LoopChangedEventArgs> e)
        {
            string value = Enum.GetName(typeof(LoopType), e.Source.Loop);
            string playlistPath = e.Source.AbsolutePath;

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


        private void OnPlaylistsPropertyChanged(object sender, SubscriptionsEventArgs<ILibrary, PlaylistsChangedEventArgs> e)
        {
            string value = XmlConverter.Serialize(library.Playlists);
            ValueSet valueSet = receivers[playlistsPropertyPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceivePlaylistsPropertyChanged(ValueSet valueSet, string value)
        {
            library.Playlists = XmlConverter.DeserializeNew<PlaylistCollection>(value);
        }


        private void OnPlaylistCollectionChanged(object sender, SubscriptionsEventArgs<IPlaylistCollection, PlaylistCollectionChangedEventArgs> e)
        {
            string removeXml = XmlConverter.Serialize(e.Base.GetRemoved().Select(p => p.AbsolutePath).ToArray());
            string addXml = XmlConverter.SerializeList(e.Base.GetAdded().ToArray());

            ValueSet valueSet = receivers[playlistsCollectionPrimaryKey].GetValueSet(string.Empty);
            valueSet.Add(removeKey, removeXml);
            valueSet.Add(addKey, addXml);

            Send(valueSet);
        }

        private void ReceivePlaylistsCollectionChanged(ValueSet valueSet, string value)
        {
            string removeXml = valueSet[removeKey].ToString();
            string addXml = valueSet[addKey].ToString();

            string[] removePaths = XmlConverter.Deserialize<string[]>(removeXml);
            Playlist[] adds = XmlConverter.DeserializeList<Playlist>(addXml).ToArray();

            List<IPlaylist> removes = new List<IPlaylist>();
            foreach (string path in removePaths)
            {
                IPlaylist playlist;

                if (HavePlaylist(path, out playlist)) removes.Add(playlist);
            }

            library.Playlists.Change(removes, adds);
        }


        private void OnCurrentPlaylistChanged(object sender, SubscriptionsEventArgs<ILibrary, CurrentPlaylistChangedEventArgs> e)
        {
            string value = e.Base.NewCurrentPlaylist.AbsolutePath;
            ValueSet valueSet = receivers[currentPlaylistPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceiveCurrentPlaylist(ValueSet valueSet, string value)
        {
            IPlaylist newCurrentPlaylist;
            if (!HavePlaylist(value, out newCurrentPlaylist)) return;

            library.CurrentPlaylist = newCurrentPlaylist;
        }


        private void OnIsPlayingChanged(object sender, SubscriptionsEventArgs<ILibrary, IsPlayingChangedEventArgs> e)
        {
            SendIsPlaying(e.Base.NewValue);
        }

        private void SendIsPlaying(bool isPlaying)
        {
            string value = isPlaying.ToString();
            ValueSet valueSet = receivers[isPlayingPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceivePlayState(ValueSet valueSet, string value)
        {
            library.IsPlaying = bool.Parse(value);
        }


        private void OnPlayerStateChanged(object sender, SubscriptionsEventArgs<ILibrary, PlayerStateChangedEventArgs> e)
        {
            SendPlayerState(e.Base.NewState);
        }

        private void SendPlayerState(MediaPlayerState playerState)
        {
            string value = Enum.GetName(typeof(MediaPlayerState), playerState);
            ValueSet valueSet = receivers[playerStatePrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceivePlayerState(ValueSet valueSet, string value)
        {
            library.PlayerState = (MediaPlayerState)Enum.Parse(typeof(MediaPlayerState), value);
        }


        private void OnLoaded(object sender, SubscriptionsEventArgs<ILibrary, EventArgs> e)
        {
            if (!library.IsForeground) SendLibrary();
            if (library.IsPlaying) SendIsPlaying(true);
        }


        public void OnSettingsChanged()
        {
            string value = string.Empty;
            ValueSet valueSet = receivers[settingsPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        public void GetLibrary()
        {
            string value = library.IsPlaying.ToString();
            ValueSet valueSet = receivers[getLibraryPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void ReceiveGetLibrary(ValueSet valueSet, string value)
        {
            if (library.IsForeground || !library.IsLoaded) return;

            if (value == true.ToString()) library.IsPlaying = true;

            SendLibrary();

        }

        private void SendLibrary()
        {
            string value = library.Playlists.Count > 0 ? XmlConverter.Serialize(library) : libraryEmptyValue;
            ValueSet valueSet = receivers[libraryPrimaryKey].GetValueSet(value);

            Send(valueSet);
            SendIsPlaying(library.IsPlaying);
            SendPlayerState(library.PlayerState);
        }

        private void ReceiveLibrary(ValueSet valueSet, string value)
        {
            ILibrary receivedLibrary = new Library(library.IsForeground);

            if (value != libraryEmptyValue) receivedLibrary.ReadXml(XmlConverter.GetReader(value));

            library.Load(receivedLibrary.Playlists);

            //if (value == libraryEmptyValue) library.Playlists.Change(library?.Playlists, null);
        }


        private void OnSkippedSongsChanged(SkipSongs sender)
        {
            string value = string.Empty;
            ValueSet valueSet = receivers[skipPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }


        private void OnSkippedSong(object sender, SubscriptionsEventArgs<SkipSongs, EventArgs> e)
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

            if (GetPrimaryKey(valueSet) != songPositionPrimaryKey)
            {
                MobileDebug.Service.WriteEvent("Send", GetPrimaryKey(valueSet), "Do: " + send, "Loaded: " + library.IsLoaded);
            }

            if (!send) return;

            senderMethod(valueSet);
        }

        private bool AllowedToSend(ValueSet valueSet)
        {
            foreach (Tuple<int, ValueSet> receivingItem in receivingItems.Where(f => f.Item1 == Environment.CurrentManagedThreadId))
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
                MobileDebug.Service.WriteEvent("ReceiveFail1", exc1, currentReceivedPrimaryKey, primaryData);
            }
        }

        private void Handle(ValueSet valueSet)
        {
            Tuple<int, ValueSet> receivingItem = new Tuple<int, ValueSet>(Environment.CurrentManagedThreadId, valueSet);
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
                foreach (Song s in playlist.Songs.Concat(playlist.Songs.Shuffle))
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

        private static IShuffleCollection GetShuffleCollection(ShuffleType type, ISongCollection songs, string xmlText)
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

                case ShuffleType.Path:
                    shuffle = new ShufflePathCollection(songs);
                    break;

                case ShuffleType.Complete:
                    shuffle = new ShuffleCompleteCollection(songs);
                    break;

                default:
                    throw new NotImplementedException();
            }

            shuffle.ReadXml(XmlConverter.GetReader(xmlText));

            return shuffle;
        }
    }
}
