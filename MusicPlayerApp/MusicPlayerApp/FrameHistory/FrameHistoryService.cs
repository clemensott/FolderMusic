using FolderMusic.FrameHistory.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using MusicPlayer.Handler;

namespace FolderMusic.FrameHistory
{
    class FrameHistoryService
    {
        private readonly Queue<HistoricFrame> restoreHistory;
        private readonly Stack<HistoricFrame> history;
        private readonly Frame rootFrame;
        private readonly ForegroundPlayerHandler handler;
        private Parameter parameter;

        public FrameHistoryService(IEnumerable<HistoricFrame> history, Frame rootFrame, ForegroundPlayerHandler handler)
        {
            restoreHistory = new Queue<HistoricFrame>(history);
            MobileDebug.Service.WriteEventPair("FrameHistoricService Constructor", "RestoreFrames", restoreHistory.Select(f => f.PageTypeName));

            this.history = new Stack<HistoricFrame>();
            this.rootFrame = rootFrame;
            this.handler = handler;

            rootFrame.Navigating += RootFrame_Navigating;
            rootFrame.Navigated += RootFrame_Navigated;
        }

        private void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            HistoricFrameHandler handler;
            HistoricParameter parameter;

            try
            {
                switch (e.NavigationMode)
                {
                    case NavigationMode.New:
                    case NavigationMode.Forward:
                        SaveDataContext();

                        handler = HistoricFrameHandler.GetHandler(e.SourcePageType);
                        if (!handler.SaveFrame) return;

                        parameter = handler.ToHistoricParameter(e.Parameter);

                        history.Push(new HistoricFrame(e.SourcePageType, parameter));
                        break;

                    case NavigationMode.Refresh:
                        handler = HistoricFrameHandler.GetHandler(e.SourcePageType);
                        if (!handler.SaveFrame) return;

                        parameter = handler.ToHistoricParameter(e.Parameter);

                        history.Pop();
                        history.Push(new HistoricFrame(e.SourcePageType, parameter));
                        break;

                    case NavigationMode.Back:
                        if (rootFrame.BackStackDepth < history.Count) history.Pop();
                        break;
                }
            }
            catch (Exception exc)
            {
                MobileDebug.Service.WriteEvent("Navigating error", exc);
                throw;
            }
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            Page page = (Page)e.Content;

            if ((parameter != null && parameter.UseDataContext) || restoreHistory.Count > 0)
            {
                page.Loaded += OnPageLoaded;
            }
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement page = (FrameworkElement)sender;

            page.Loaded -= OnPageLoaded;

            if (parameter != null && parameter.UseDataContext)
            {
                page.DataContext = parameter.DataContext;
            }

            Restore();
        }

        public bool Restore()
        {
            if (restoreHistory.Count == 0) return false;

            HistoricFrame frame = restoreHistory.Dequeue();
            HistoricFrameHandler handler = HistoricFrameHandler.GetHandler(frame.Page);
            parameter = handler.FromHistoricParameter(frame.Parameter, this.handler);

            try
            {
                return rootFrame.Navigate(frame.Page, parameter.Value);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("RestoreFail", e, frame.PageTypeName, parameter.Value);
                restoreHistory.Clear();
                return false;
            }
        }

        private void SaveDataContext()
        {
            if (history.Count == 0) return;

            HistoricFrame frame = history.Last();

            if (frame.Parameter.UseDataContext)
            {
                HistoricFrameHandler handler = HistoricFrameHandler.GetHandler(frame.Page);
                frame.Parameter.DataContext = handler.ToHistoricDataContext(GetCurrentPage().DataContext);
            }
        }

        private Page GetCurrentPage()
        {
            return rootFrame.Content as Page;
        }

        public IEnumerable<HistoricFrame> GetFrames()
        {
            return history;
        }
    }
}
