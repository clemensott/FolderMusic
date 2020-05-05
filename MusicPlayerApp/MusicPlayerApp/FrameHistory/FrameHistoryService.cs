using FolderMusic.FrameHistory.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using MusicPlayer.Models.Interfaces;

namespace FolderMusic.FrameHistory
{
    class FrameHistoryService
    {
        private readonly Queue<HistoricFrame> restoreHistory;
        private readonly Stack<HistoricFrame> history;
        private readonly Frame rootFrame;
        private readonly ILibrary library;
        private Parameter parameter;

        public FrameHistoryService(IEnumerable<HistoricFrame> history, Frame rootFrame, ILibrary library)
        {
            restoreHistory = new Queue<HistoricFrame>(history);
            MobileDebug.Service.WriteEventPair("FrameHistoricService Constructor", "RestoreFrames", restoreHistory.Count);

            this.history = new Stack<HistoricFrame>();
            this.rootFrame = rootFrame;
            this.library = library;

            rootFrame.Navigating += RootFrame_Navigating;
            rootFrame.Navigated += RootFrame_Navigated;
        }

        private void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            MobileDebug.Service.WriteEventPair("Navigating1_0", "Page", e.SourcePageType, "Mode", e.NavigationMode,
                "Parameter", e.Parameter ?? "null", "TransInfo", e.NavigationTransitionInfo,
                "frames", history.Reverse().Select(f => f?.PageTypeName));

            HistoricFrameHandler handler;
            HistoricParameter parameter;

            switch (e.NavigationMode)
            {
                case NavigationMode.New:
                case NavigationMode.Forward:
                    SaveDataContext();

                    handler = HistoricFrameHandler.GetHandler(e.SourcePageType);
                    parameter = handler.ToHistoricParameter(e.Parameter);

                    history.Push(new HistoricFrame(e.SourcePageType, parameter));
                    break;

                case NavigationMode.Refresh:
                    handler = HistoricFrameHandler.GetHandler(e.SourcePageType);
                    parameter = handler.ToHistoricParameter(e.Parameter);

                    history.Pop();
                    history.Push(new HistoricFrame(e.SourcePageType, parameter));
                    break;

                case NavigationMode.Back:
                    history.Pop();
                    break;
            }
            MobileDebug.Service.WriteEvent("Navigating2", history.Reverse().Select(f => f?.PageTypeName));
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            MobileDebug.Service.WriteEvent("RootFrame_Navigated", e.Content);

            Page page = (Page)e.Content;

            if ((parameter != null && parameter.UseDataContext) || restoreHistory.Count > 0)
            {
                page.Loaded += OnPageLoaded;
            }
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            MobileDebug.Service.WriteEvent("Page_Loaded", sender.GetType());

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
            MobileDebug.Service.WriteEvent("Restore1", restoreHistory.Select(f => f.PageTypeName));
            //return false;

            if (restoreHistory.Count == 0) return false;

            HistoricFrame frame = restoreHistory.Dequeue();
            HistoricFrameHandler handler = HistoricFrameHandler.GetHandler(frame.Page);
            parameter = handler.FromHistoricParameter(frame.Parameter, library);

            try
            {
                MobileDebug.Service.WriteEventPair("Restore2", "Page", frame.PageTypeName, "Parameter", parameter?.Value);
                //rootFrame.Navigate(typeof(MobileDebug.DebugPage), null);
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
