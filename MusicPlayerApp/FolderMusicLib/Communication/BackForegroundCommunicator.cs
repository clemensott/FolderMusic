using MusicPlayer.Data;
using MusicPlayer.Data.Loop;
using MusicPlayer.Data.Shuffle;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.Media.Playback;

namespace MusicPlayer.Communication
{
    class BackForegroundCommunicator
    {
        protected const string playlistsIndexKey = "PlaylistIndex", songsIndexKey = "SongsIndex",
            songPathKey = "SongPath", playlistPathKey = "PlaylistPath",
            ArtistPrimaryKey = "ArtistPrimary",
            TitlePrimaryKey = "TitlePrimary",
            DurationPrimaryKey = "DurationPrimary",
            songPositionPrimaryKey = "SongPositionPrimary",
            playlistPrimaryKey = "PlaylistPrimary",
            currentSongPrimaryKey = "CurrentSongPrimary",
            songsPrimaryKey = "SongsPrimary",
            removeSongPrimaryKey = "RemoveSongPrimary",
            shufflePrimaryKey = "ShufflePrimary",
            shuffleListPrimaryKey = "ShuffleListPrimary",
            loopPrimaryKey = "LoopPrimary",
            libraryPrimaryKey = "LibraryPrimary",
            playlistsPrimaryKey = "PlaylistsPrimary",
            removePlaylistPrimaryKey = "removePlaylistPrimary",
            currentPlaylistPrimaryKey = "CurrentPlaylistPrimary",
            settingsPrimaryKey = "SettingsPrimary",
            playStatePrimaryKey = "PlayStatePrimary",
            getLibraryPrimaryKey = "GetLibraryPrimary",
            skipPrimaryKey = "SkipPrimary",
            songPositionKey = "SongPosition",
            shuffleKey = "Shuffle",
            shuffleListKey = "ShuffleList",
            currentPlaylistKey = "CurentPlaylist",
            libraryEmptyValue = "libraryEmpty";
        private static readonly TimeSpan minResendKeyTimeSpan = TimeSpan.FromMilliseconds(300);

        private static BackForegroundCommunicator instance;

        public static void StartCommunication(bool isForeground)
        {
            FolderMusicDebug.DebugEvent.SaveText("StartCommunication", "IsForeground: " + isForeground, "Existing: " + (instance != null));
            instance = new BackForegroundCommunicator(isForeground);
        }

        private bool isForeground;
        private List<Tuple<int, ValueSet>> receivingItems;
        private Action<ValueSet> senderMethod;
        private Dictionary<string, Receiver> receivers;

        private BackForegroundCommunicator(bool isForeground)
        {
            receivingItems = new List<Tuple<int, ValueSet>>();

            this.isForeground = isForeground;
            receivers = GetAllReceiver().ToDictionary(r => r.Key);

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

            Feedback.Current.OnArtistPropertyChanged += OnArtistPropertyChanged;
            Feedback.Current.OnTitlePropertyChanged += OnTitlePropertyChanged;
            Feedback.Current.OnCurrentPlaylistPropertyChanged += OnCurrentPlaylistPropertyChanged;
            Feedback.Current.OnCurrentSongPropertyChanged += OnCurrentSongPropertyChanged;
            Feedback.Current.OnLibraryChanged += OnLibraryChanged;
            Feedback.Current.OnLoopPropertyChanged += OnLoopPropertyChanged;
            Feedback.Current.OnPlaylistsPropertyChanged += OnPlaylistsPropertyChanged;
            Feedback.Current.OnSettingsPropertyChanged += OnSettingsPropertyChanged;
            Feedback.Current.OnShufflePropertyChanged += OnShufflePropertyChanged;
            Feedback.Current.OnSkippedSongsPropertyChanged += OnSkippedSongsPropertyChanged;
            Feedback.Current.OnSongsPropertyChanged += OnSongsPropertyChanged;
            Feedback.Current.OnPlayStateChanged += OnPlayStateChanged;
        }

        private IEnumerable<Receiver> GetAllReceiver()
        {
            yield return new Receiver(TitlePrimaryKey, new Action<ValueSet, string>(ReceiveSongTitle));
            yield return new Receiver(ArtistPrimaryKey, new Action<ValueSet, string>(ReceiveSongArtist));
            yield return new Receiver(DurationPrimaryKey, new Action<ValueSet, string>(ReceiveSongDuration));
            yield return new Receiver(songPositionPrimaryKey, new Action<ValueSet, string>(ReceiveSongPosition));
            yield return new Receiver(playlistPrimaryKey, new Action<ValueSet, string>(ReceivePlaylist));
            yield return new Receiver(currentSongPrimaryKey, new Action<ValueSet, string>(ReceiveCurrentSong));
            yield return new Receiver(songsPrimaryKey, new Action<ValueSet, string>(ReceiveSongs));
            yield return new Receiver(shufflePrimaryKey, new Action<ValueSet, string>(ReceiveShuffle));
            yield return new Receiver(loopPrimaryKey, new Action<ValueSet, string>(ReceiveLoop));
            yield return new Receiver(libraryPrimaryKey, new Action<ValueSet, string>(ReceiveLibrary));
            yield return new Receiver(playlistsPrimaryKey, new Action<ValueSet, string>(ReceivePlaylists));
            yield return new Receiver(currentPlaylistPrimaryKey, new Action<ValueSet, string>(ReceiveCurrentPlaylist));
            yield return new Receiver(settingsPrimaryKey, new Action<ValueSet, string>(ReceiveSettings));
            yield return new Receiver(playStatePrimaryKey, new Action<ValueSet, string>(ReceivePlayState));
            yield return new Receiver(getLibraryPrimaryKey, new Action<ValueSet, string>(ReceiveGetLibrary));
            yield return new Receiver(skipPrimaryKey, new Action<ValueSet, string>(ReceiveSkip));
        }

        private void OnArtistPropertyChanged(Song sender, SongArtistChangedEventArgs args)
        {
            int playlistsIndex, songsIndex;

            if (!Library.Base.HavePlaylistIndexAndSongsIndex(sender, out playlistsIndex, out songsIndex)) return;

            string path = sender.Path;
            string value = sender.Artist;

            ValueSet valueSet = receivers[ArtistPrimaryKey].GetValueSet(value);
            valueSet.Add(playlistsIndexKey, playlistsIndex.ToString());
            valueSet.Add(songsIndexKey, songsIndex.ToString());
            valueSet.Add(songPathKey, path);

            Send(valueSet);
        }

        private void OnTitlePropertyChanged(Song sender, SongTitleChangedEventArgs args)
        {
            int playlistsIndex, songsIndex;

            if (!Library.Base.HavePlaylistIndexAndSongsIndex(sender, out playlistsIndex, out songsIndex)) return;

            string path = sender.Path;
            string value = sender.Title;

            ValueSet valueSet = receivers[TitlePrimaryKey].GetValueSet(value);
            valueSet.Add(playlistsIndexKey, playlistsIndex.ToString());
            valueSet.Add(songsIndexKey, songsIndex.ToString());
            valueSet.Add(songPathKey, path);

            Send(valueSet);
        }

        private void OnNaturalDurationPropertyChanged(Song sender, SongNaturalDurationChangedEventArgs args)
        {
            int playlistsIndex, songsIndex;

            if (!Library.Base.HavePlaylistIndexAndSongsIndex(sender, out playlistsIndex, out songsIndex)) return;

            string path = sender.Path;
            double value = sender.NaturalDurationMilliseconds;

            ValueSet valueSet = receivers[DurationPrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(playlistsIndexKey, playlistsIndex.ToString());
            valueSet.Add(songsIndexKey, songsIndex.ToString());
            valueSet.Add(songPathKey, path);

            Send(valueSet);
        }

        public void OnCurrentSongPositionPropertyChanged(Playlist sender, CurrentSongPositionChangedEventArgs args)
        {
            return;
            int playlistsIndex = Library.Current.Playlists.IndexOf(sender);
            string value = sender.SongPositionPercent.ToString();
            string path = sender.AbsolutePath;

            ValueSet valueSet = receivers[songPositionPrimaryKey].GetValueSet(value);
            valueSet.Add(playlistPathKey, path);
            valueSet.Add(playlistsIndexKey, playlistsIndex.ToString());

            Send(valueSet);
        }

        public void OnCurrentSongPropertyChanged(Playlist sender, CurrentSongChangedEventArgs args)
        {
            string value = sender.SongsIndex.ToString();
            int playlistsIndex = sender.PlaylistIndex;
            string path = sender.CurrentSong.Path;

            ValueSet valueSet = receivers[currentSongPrimaryKey].GetValueSet(value);
            valueSet.Add(songPathKey, path);
            valueSet.Add(playlistsIndexKey, playlistsIndex.ToString());

            Send(valueSet);
        }

        public void OnSongsPropertyChanged(Playlist sender, SongsChangedEventArgs args)
        {
            string value = XmlConverter.Serialize(sender.Songs);
            string shuffle = sender.Shuffle.ToString();
            string shuffleList = XmlConverter.Serialize(sender.ShuffleList);
            int songsIndex = sender.SongsIndex;
            double position = sender.SongPositionPercent;
            int playlistsIndex = sender.PlaylistIndex;
            string path = sender.AbsolutePath;

            ValueSet valueSet = receivers[songsPrimaryKey].GetValueSet(value);
            valueSet.Add(shuffleKey, shuffle.ToString());
            valueSet.Add(shuffleListKey, shuffleList);
            valueSet.Add(songsIndexKey, songsIndex.ToString());
            valueSet.Add(songPositionKey, position.ToString());
            valueSet.Add(playlistsIndexKey, playlistsIndex.ToString());
            valueSet.Add(playlistPathKey, path);

            Send(valueSet);
        }

        public void OnShufflePropertyChanged(Playlist sender, ShuffleChangedEventArgs args)
        {
            string value = sender.Shuffle.ToString();
            string shuffleList = XmlConverter.Serialize(sender.ShuffleList);
            int songsIndex = sender.SongsIndex;
            int playlistsIndex = sender.PlaylistIndex;
            string path = sender.AbsolutePath;

            ValueSet valueSet = receivers[shufflePrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(shuffleListKey, shuffleList);
            valueSet.Add(songsIndexKey, songsIndex.ToString());
            valueSet.Add(playlistsIndexKey, playlistsIndex.ToString());
            valueSet.Add(playlistPathKey, path);

            Send(valueSet);
        }

        public void OnLoopPropertyChanged(Playlist sender, LoopChangedEventArgs args)
        {
            string value = sender.Loop.ToString();
            int playlistsIndex = sender.PlaylistIndex;
            string path = sender.AbsolutePath;

            ValueSet valueSet = receivers[loopPrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(playlistPathKey, path);
            valueSet.Add(playlistsIndexKey, playlistsIndex.ToString());

            Send(valueSet);
        }

        private void OnLibraryChanged(ILibrary sender, LibraryChangedEventsArgs args)
        {
            if (isForeground) return;
            if (!Library.IsLoaded(sender)) return;

            string value = Library.Base.IsEmpty ? value = libraryEmptyValue : Library.Base.GetXmlText();

            ValueSet valueSet = receivers[libraryPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void OnPlaylistsPropertyChanged(ILibrary sender, PlaylistsChangedEventArgs args)
        {
            string value = XmlConverter.Serialize(sender.Playlists);
            int currentPlaylistIndex = sender.CurrentPlaylistIndex;
            string path = sender.CurrentPlaylist.AbsolutePath;

            ValueSet valueSet = receivers[playlistsPrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(playlistPathKey, path);
            valueSet.Add(currentPlaylistKey, currentPlaylistIndex.ToString());

            Send(valueSet);
        }

        public void OnCurrentPlaylistPropertyChanged(ILibrary sender, CurrentPlaylistChangedEventArgs args)
        {
            int value = sender.CurrentPlaylistIndex;
            string path = sender.CurrentPlaylist.AbsolutePath;

            ValueSet valueSet = receivers[currentPlaylistPrimaryKey].GetValueSet(value.ToString());
            valueSet.Add(playlistPathKey, path);

            Send(valueSet);
        }

        public void OnSettingsPropertyChanged()
        {
            string value = string.Empty;

            ValueSet valueSet = receivers[settingsPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        public void OnPlayStateChanged(ILibrary sender, PlayStateChangedEventArgs args)
        {
            string value = args.NewValue.ToString();

            ValueSet valueSet = receivers[playStatePrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        public void GetLibrary()
        {
            string value = string.Empty;

            ValueSet valueSet = receivers[getLibraryPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }

        private void OnSkippedSongsPropertyChanged(SkipSongs sender)
        {
            string value = string.Empty;

            ValueSet valueSet = receivers[skipPrimaryKey].GetValueSet(value);

            Send(valueSet);
        }


        private void Send(ValueSet valueSet)
        {
            if (AllowedToSend(valueSet)) senderMethod(valueSet);
        }

        private bool AllowedToSend(ValueSet valueSet)
        {
            var receivingItem = receivingItems.FirstOrDefault(f => f.Item1 == Environment.CurrentManagedThreadId);

            if (receivingItem == null) return true;

            return Same(valueSet, receivingItem.Item2);
        }

        private bool Same(ValueSet valueSet1, ValueSet valueSet2)
        {
            if (valueSet1.Count != valueSet2.Count) return true;

            for (int i = 0; i < valueSet1.Count; i++)
            {
                if (valueSet1.ElementAt(i).Key != valueSet2.ElementAt(i).Key) return true;
            }

            return false;
        }

        private void BackgroundMediaPlayer_MessageReceived(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            var receivingItem = new Tuple<int, ValueSet>(Environment.CurrentManagedThreadId, e.Data);
            receivingItems.Add(receivingItem);

            try
            {
                string currentReceivedPrimaryKey = GetPrimaryKey(e.Data);

                //System.Diagnostics.Debug.WriteLine("IsForeground: {0},Key: {1}, Time: {2}",
                //    isForeground, currentReceivedPrimaryKey, DateTime.Now.ToString("hh:mm:ss.fff"));

                try
                {
                    receivers[currentReceivedPrimaryKey].Handle(e.Data);

                    string primaryData = e.Data[currentReceivedPrimaryKey].ToString();
                    if (primaryData.Length > 100) primaryData = primaryData.Remove(100);
                    FolderMusicDebug.DebugEvent.SaveText("Communicated", currentReceivedPrimaryKey, primaryData);
                }
                catch (Exception exc1)
                {
                    string primaryData = e.Data[currentReceivedPrimaryKey].ToString();
                    FolderMusicDebug.DebugEvent.SaveText("CommunicationFailed", currentReceivedPrimaryKey, primaryData);
                }
            }
            catch (Exception exc2)
            {
                //try
                //{
                //    foreach (var pair in e.Data)
                //    {
                //        System.Diagnostics.Debug.WriteLine("Pair: ");
                //        System.Diagnostics.Debug.WriteLine("Key: {0}", pair.Key ?? "Null");

                //        string value = (string)pair.Value ?? "Null";
                //        if (value.Length > 100) value = value.Remove(100);

                //        System.Diagnostics.Debug.WriteLine("Value: {0}", value);
                //    }
                //}
                //catch (Exception exc3)
                //{
                //}
            }

            receivingItems.Remove(receivingItem);
        }

        private string GetPrimaryKey(ValueSet valueSet)
        {
            return valueSet.Keys.FirstOrDefault(k => k.Contains("Primary"));
        }


        private void ReceiveSongTitle(ValueSet valueSet, string value)
        {
            int playlistsIndex = int.Parse(valueSet[playlistsIndexKey].ToString());
            int songsIndex = int.Parse(valueSet[songsIndexKey].ToString());
            string path = valueSet[songPathKey].ToString();

            if (!HavePlaylistAndSongsIndex(ref playlistsIndex, ref songsIndex, path)) return;

            Library.Current[playlistsIndex].Songs[songsIndex].Title = value;
        }

        private void ReceiveSongArtist(ValueSet valueSet, string value)
        {
            int playlistsIndex = int.Parse(valueSet[playlistsIndexKey].ToString());
            int songsIndex = int.Parse(valueSet[songsIndexKey].ToString());
            string path = valueSet[songPathKey].ToString();

            if (!HavePlaylistAndSongsIndex(ref playlistsIndex, ref songsIndex, path)) return;

            Library.Current[playlistsIndex].Songs[songsIndex].Artist = value;
        }

        private void ReceiveSongDuration(ValueSet valueSet, string value)
        {
            double duration = double.Parse(value);
            int playlistsIndex = int.Parse(valueSet[playlistsIndexKey].ToString());
            int songsIndex = int.Parse(valueSet[songsIndexKey].ToString());
            string path = valueSet[songPathKey].ToString();

            if (!HavePlaylistAndSongsIndex(ref playlistsIndex, ref songsIndex, path)) return;

            Library.Current[playlistsIndex].Songs[songsIndex].NaturalDurationMilliseconds = duration;
        }

        private void ReceiveSongPosition(ValueSet valueSet, string value)
        {
            return;
            double position = double.Parse(value);
            int playlistsIndex = int.Parse(valueSet[playlistsIndexKey].ToString());
            string path = valueSet[playlistPathKey].ToString();

            if (!HavePlaylistIndex(ref playlistsIndex, path)) return;

            Library.Current[playlistsIndex].SongPositionPercent = position;
        }

        private void ReceivePlaylist(ValueSet valueSet, string value)
        {
            Playlist playlist = XmlConverter.Deserialize<Playlist>(value);
            int playlistsIndex = int.Parse(valueSet[playlistsIndexKey].ToString());
            string path = valueSet[playlistPathKey].ToString();

            if (!HavePlaylistIndex(ref playlistsIndex, path)) return;

            Library.Current[playlistsIndex] = playlist;
        }

        private void ReceiveCurrentSong(ValueSet valueSet, string value)
        {
            int songsIndex = int.Parse(value);
            int playlistsIndex = int.Parse(valueSet[playlistsIndexKey].ToString());
            string path = valueSet[songPathKey].ToString();

            if (!HavePlaylistAndSongsIndex(ref playlistsIndex, ref songsIndex, path)) return;

            Library.Current[playlistsIndex].SongsIndex = songsIndex;
        }

        private void ReceiveSongs(ValueSet valueSet, string value)
        {
            SongList songs = XmlConverter.Deserialize<SongList>(value);
            ShuffleType shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType), valueSet[shuffleKey].ToString());
            List<int> shuffleList = XmlConverter.Deserialize<List<int>>(valueSet[shuffleListKey].ToString());
            int songsIndex = int.Parse(valueSet[songsIndexKey].ToString());
            double position = double.Parse(valueSet[songPositionKey].ToString());
            int playlistsIndex = int.Parse(valueSet[playlistsIndexKey].ToString());
            string path = valueSet[playlistPathKey].ToString();

            if (!HavePlaylistIndex(ref playlistsIndex, path)) return;

            Playlist playlist = Library.Current[playlistsIndex];

            playlist.SetSongs(songs, shuffle, shuffleList, songs[songsIndex], position);
        }

        private void ReceiveShuffle(ValueSet valueSet, string value)
        {
            ShuffleType shuffle = (ShuffleType)Enum.Parse(typeof(ShuffleType), valueSet[shuffleKey].ToString());
            List<int> shuffleList = XmlConverter.Deserialize<List<int>>(valueSet[shuffleListKey].ToString());
            int songsIndex = int.Parse(valueSet[songsIndexKey].ToString());
            int playlistsIndex = int.Parse(valueSet[playlistsIndexKey].ToString());
            string path = valueSet[playlistPathKey].ToString();

            if (!HavePlaylistIndex(ref playlistsIndex, path)) return;

            Playlist playlist = Library.Current[playlistsIndex];

            playlist.SetSongs(playlist.Songs, shuffle, shuffleList, playlist[songsIndex], playlist.SongPositionPercent);
        }

        private void ReceiveLoop(ValueSet valueSet, string value)
        {
            LoopType loop = (LoopType)Enum.Parse(typeof(LoopType), value);
            int playlistsIndex = int.Parse(valueSet[playlistsIndexKey].ToString());
            string path = valueSet[playlistPathKey].ToString();

            if (!HavePlaylistIndex(ref playlistsIndex, path)) return;

            Library.Current[playlistsIndex].Loop = loop;
        }

        private void ReceiveLibrary(ValueSet valueSet, string value)
        {
            if (value == libraryEmptyValue)
            {
                Library.nonLoadedInstance = null;

                CurrentPlaySong.Current.Unset();
                Library.Current.SkippedSongs.Delete();

                Feedback.Current.RaiseLibraryChanged(Library.Current, Library.Current);
            }
            else Library.Load(value);
        }

        private void ReceivePlaylists(ValueSet valueSet, string value)
        {
            PlaylistList playlists = XmlConverter.Deserialize<PlaylistList>(value);
            int playlistsIndex = int.Parse(valueSet[currentPlaylistKey].ToString());
            string path = playlists[playlistsIndex].AbsolutePath;

            if (!HavePlaylistIndex(ref playlistsIndex, path)) return;

            Library.Data?.SetPlaylists(playlists, playlists[playlistsIndex]);
        }

        private void ReceiveCurrentPlaylist(ValueSet valueSet, string value)
        {
            int currentPlaylistIndex = int.Parse(value);
            string path = valueSet[playlistPathKey].ToString();

            if (!HavePlaylistIndex(ref currentPlaylistIndex, path)) return;

            Library.Current.CurrentPlaylistIndex = currentPlaylistIndex;
        }

        private void ReceiveSettings(ValueSet valueSet, string value)
        {
            Feedback.Current.RaiseSettingsPropertyChanged();
        }

        private void ReceivePlayState(ValueSet valueSet, string value)
        {
            Library.Current.IsPlaying = bool.Parse(value);
        }

        private void ReceiveGetLibrary(ValueSet valueSet, string value)
        {
            OnLibraryChanged(Library.Current, new LibraryChangedEventsArgs(Library.Current, Library.Current));
        }

        private void ReceiveSkip(ValueSet valueSet, string value)
        {
            if (isForeground) Feedback.Current.RaiseSkippedSongsPropertyChanged();
        }

        private bool HavePlaylistIndex(ref int playlistsIndex, string path)
        {
            return (playlistsIndex >= 0 && playlistsIndex < Library.Current.Playlists.Count &&
                Library.Current[playlistsIndex].AbsolutePath != path) ||
                Library.Base.HavePlaylistIndex(path, out playlistsIndex);
        }

        private bool HavePlaylistAndSongsIndex(ref int playlistsIndex, ref int songsIndex, string path)
        {
            return (playlistsIndex < 0 && playlistsIndex >= Library.Current.Playlists.Count &&
                songsIndex < 0 && songsIndex >= Library.Current[playlistsIndex].Songs.Count &&
                Library.Current[playlistsIndex][songsIndex].Path != path) ||
                Library.Base.HavePlaylistIndexAndSongsIndex(path, out playlistsIndex, out songsIndex);
        }
    }
}
