using FolderMusic.FrameHistory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using MusicPlayer.Handler;
using MusicPlayer.Models.Foreground;
using MusicPlayer.Models.Foreground.Interfaces;

namespace FolderMusic
{
    public sealed partial class App : Application
    {
        private const string libraryDataFileName = "data.xml", frameHistoryFileName = "FrameHistory.xml";
        private static readonly XmlSerializer frameHistorySerializer = new XmlSerializer(typeof(HistoricFrame[]));

        private ForegroundPlayerHandler handler;
        private Frame rootFrame;
        private TransitionCollection transitions;
        private FrameHistoryService frameHistoryService;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;

            MobileDebug.Service.SetIsForeground();

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            UnhandledException += App_UnhandledException;

            DebugSettings.BindingFailed += (sender, args) =>
            {
                MobileDebug.Service.WriteEvent("Binding error", args.Message);
            };
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            MobileDebug.Service.WriteEvent("HardwareButtons_BackPressed", rootFrame.CurrentSourcePageType);

            e.Handled = true;

            if (rootFrame.CurrentSourcePageType == typeof(LockPage)) return;
            if (!rootFrame.CanGoBack) Current.Exit();
            else rootFrame.GoBack();
        }

        private static void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MobileDebug.Service.WriteEvent("UnhandledException", e.Exception, e.Exception.StackTrace);
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            if (handler == null)
            {
                ILibrary library = await Library.Load(libraryDataFileName);
                handler = new ForegroundPlayerHandler(library);
            }

            rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.CacheSize = 1;
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                MobileDebug.Service.WriteEventPair("OnLaunched1", "PreviousExecutionState", e.PreviousExecutionState);
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Zustand von zuvor angehaltener Anwendung laden
                    IEnumerable<HistoricFrame> frameHistory = await ReadHistoricFrames();
                    frameHistoryService = new FrameHistoryService(frameHistory, rootFrame, handler);
                }
                else
                {
                    frameHistoryService =
                        new FrameHistoryService(Enumerable.Empty<HistoricFrame>(), rootFrame, handler);
                }

                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (Transition c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;

                if (!frameHistoryService.Restore() && !rootFrame.Navigate(typeof(MainPage), handler))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            Window.Current.Activate();
            Window.Current.Activated += Window_Activated;
        }

        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            rootFrame.ContentTransitions =
                this.transitions ?? new TransitionCollection() {new NavigationThemeTransition()};
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        private async void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            MobileDebug.Service.WriteEvent("Window_Activated", e.WindowActivationState);
            switch (e.WindowActivationState)
            {
                case CoreWindowActivationState.Deactivated:
                    handler.Stop();
                    await Library.Save(libraryDataFileName, handler.Library);
                    await WriteHistoricFrames(frameHistoryService.GetFrames().Reverse().ToArray());
                    break;

                case CoreWindowActivationState.CodeActivated:
                case CoreWindowActivationState.PointerActivated:
                    handler.Start();
                    break;
            }
        }

        private static async Task<IEnumerable<HistoricFrame>> ReadHistoricFrames()
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(frameHistoryFileName);
                string frameHistoryXml = await FileIO.ReadTextAsync(file);

                return (IEnumerable<HistoricFrame>)frameHistorySerializer.Deserialize(
                    new StringReader(frameHistoryXml));
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("ReadHistoricFrame", e);

                return Enumerable.Empty<HistoricFrame>();
            }
        }

        private static async Task WriteHistoricFrames(HistoricFrame[] frames)
        {
            string frameHistoryXml;
            try
            {
                StringWriter writer = new StringWriter();
                frameHistorySerializer.Serialize(writer, frames);
                frameHistoryXml = writer.ToString();
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SerializeHistoricFrames", e, frames.Length);
                return;
            }

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder
                    .CreateFileAsync(frameHistoryFileName, CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, frameHistoryXml);
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("WriteHistoricFramesError", e, frames.Length);
            }
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }
    }
}
