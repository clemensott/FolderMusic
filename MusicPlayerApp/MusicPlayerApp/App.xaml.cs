using FolderMusic.FrameHistory;
using MusicPlayer.Data;
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

namespace FolderMusic
{
    public sealed partial class App : Application
    {
        private const string simpleFileName = "SimpleData.xml", frameHistoryFileName = "FrameHistory.xml";
        private static readonly XmlSerializer frameHistorySerializer = new XmlSerializer(typeof(HistoricFrame[]));

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
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            MobileDebug.Service.WriteEvent("HardwareButtons_BackPressed", rootFrame.CurrentSourcePageType);

            e.Handled = true;

            if (rootFrame.CurrentSourcePageType == typeof(LockPage)) return;
            if (!rootFrame.CanGoBack) Application.Current.Exit();
            else rootFrame.GoBack();
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MobileDebug.Service.WriteEvent("UnhandledException", e.Exception, e.Exception.StackTrace);
        }

        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            AutoSaveLoad asl = new AutoSaveLoad(null, null, simpleFileName, null);
            ILibrary library = await asl.LoadSimple(true);

            rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.CacheSize = 1;
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                MobileDebug.Service.WriteEventPair("OnLaunched", "PreviousExecutionState", e.PreviousExecutionState);
                //if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Zustand von zuvor angehaltener Anwendung laden
                    //IEnumerable<HistoricFrame> frameHistory = await ReadHistoricFrames();

                    //frameHistoryService = new FrameHistoryService(frameHistory, rootFrame, library);
                }
                //else
                {
                    frameHistoryService = new FrameHistoryService(Enumerable.Empty<HistoricFrame>(), rootFrame, library);
                }

                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;

                if (!frameHistoryService.Restore() && !rootFrame.Navigate(typeof(MainPage), library))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            Window.Current.Activate();
            Window.Current.Activated += Window_Activated;
        }

        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        private async void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState != CoreWindowActivationState.Deactivated) return;

            await WriteHistoricFrames(frameHistoryService.GetFrames().Reverse().ToArray());
        }

        private async Task<IEnumerable<HistoricFrame>> ReadHistoricFrames()
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(frameHistoryFileName);
                string frameHistoryXml = await FileIO.ReadTextAsync(file);

                return (IEnumerable<HistoricFrame>)frameHistorySerializer.Deserialize(new StringReader(frameHistoryXml));
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("ReadHistoricFrame", e);

                return Enumerable.Empty<HistoricFrame>();
            }
        }

        private async Task WriteHistoricFrames(HistoricFrame[] frames)
        {
            string frameHistoryXml;

            MobileDebug.Service.WriteEvent("WriteHistoricFrames", frames?.Select(f => f?.PageTypeName));

            try
            {
                StringWriter writer = new StringWriter();
                frameHistorySerializer.Serialize(writer, frames);
                frameHistoryXml = writer.ToString();
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("SerializeHistoricFarmes", e, frames.Count());
                return;
            }

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(frameHistoryFileName);
                await FileIO.WriteTextAsync(file, frameHistoryXml);
            }
            catch (FileNotFoundException e1)
            {
                try
                {
                    StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(frameHistoryFileName);
                    await FileIO.WriteTextAsync(file, frameHistoryXml);
                }
                catch (Exception e2)
                {
                    MobileDebug.Service.WriteEvent("CreateHistoricFramesFile", e2, frames.Count());
                }
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("WriteHistoricFarmes", e, frames.Count());
            }
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }
    }
}