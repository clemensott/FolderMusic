﻿using System;
using MusicPlayer.Handler;

namespace FolderMusic.FrameHistory.Handlers
{
    class HistoricFrameHandler
    {
        public virtual bool SaveFrame => true;

        public virtual HistoricParameter ToHistoricParameter(object parameter)
        {
            return new HistoricParameter(parameter);
        }

        public virtual object ToHistoricDataContext(object dataContext)
        {
            return null;
        }

        public virtual Parameter FromHistoricParameter(HistoricParameter parameter, ForegroundPlayerHandler handler)
        {
            return new Parameter(parameter.Value);
        }

        public static HistoricFrameHandler GetHandler(Type page)
        {
            if (page == typeof(MainPage)) return new MainPageHandler();
            if (page == typeof(PlaylistPage)) return new PlaylistPageHandler();
            if (page == typeof(SettingsPage)) return new SettingsPageHandler();
            if (page == typeof(SkipSongsPage)) return new SkippedSongsPageHandler();
            if (page == typeof(SongPage)) return new SongPageHandler();
            if (page == typeof(UpdateProgressPage)) return new DontSaveHandler();

            return new HistoricFrameHandler();
        }
    }
}
