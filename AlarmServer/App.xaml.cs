using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace AlarmServer
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private Geolocator locator;
        DisplayRequest displayRequest = new DisplayRequest();
        private TransitionCollection transitions;

        const string ALARM_ON_REQ = "/alarmOn";
        const string ALARM_OFF_REQ = "/alarmOff";
        public readonly MainViewModel MainModel;
        readonly Uri indexWebPageUri = new Uri("ms-appx:///Html/index.html");
        readonly WebServer webServer;
        Frame frame;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;

            MainModel = new MainViewModel();

            locator = new Geolocator()
            {
                DesiredAccuracy = PositionAccuracy.High,
                ReportInterval = 1000,
                MovementThreshold = 1,
                DesiredAccuracyInMeters = 1
            };

            webServer = new WebServer(new Dictionary<string, RuleDeletage>
            {
                { "/", HandleGetPage }
            }, "42564");
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            //locator.StatusChanged += Locator_StatusChanged;
            //locator.PositionChanged += Locator_PositionChanged;

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                frame = rootFrame;

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                displayRequest.RequestActive();

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
                Window.Current.VisibilityChanged += Current_VisibilityChanged;
            }

            if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
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

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();

            webServer.StartServer();
        }

        private void Current_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            Debug.WriteLine("VisibilityChanged: " + e.Visible);
            
            //locator.StatusChanged -= Locator_StatusChanged;
            //locator.PositionChanged -= Locator_PositionChanged;

            if (e.Visible)
            {
                
            }
            else
            {
                //locator.StatusChanged += Locator_StatusChanged;
                //locator.PositionChanged += Locator_PositionChanged;
            }
        }

        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void Locator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            Debug.WriteLine("Locator_StatusChanged: " + args.Status);
        }

        private void Locator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            Debug.WriteLine("Locator_PositionChanged: " + args.Position.Coordinate);
        }

        private async Task<UIWebResponse> HandleUIRequestAsync(UIWebRequest request)
        {
            var dispatcher = frame?.Dispatcher;
            if (dispatcher == null)
                throw new Exception("No dispacher");

            UIWebResponse response = null;

            await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                response = MainModel.HandleWebRequest(request);
            });

            return response;
        }

        private async Task<WebResponse> HandleGetPage(WebResponse request)
        {
            var uiReq = new UIWebRequest();

            if (request.uri == ALARM_ON_REQ)
                uiReq.AlarmOn = true;
            else if (request.uri == ALARM_OFF_REQ)
                uiReq.AlarmOn = false;

            var uiRes = await HandleUIRequestAsync(uiReq);

            WebResponse response = new WebResponse();
            response.header = new Dictionary<string, string>()
            {
                { "Content-Type", "text/html" },
            };

            if (uiReq.AlarmOn != null)
            {
                response.header.Add("Location", "/");
            }

            var file = await StorageFile.GetFileFromApplicationUriAsync(indexWebPageUri);
            string text = null;

            using (var fileStream = await file.OpenReadAsync())
            {
                using (var reader = new StreamReader(fileStream.AsStreamForRead()))
                {
                    text = await reader.ReadToEndAsync();

                    var alarmOnHref = uiRes.AlarmOn ? ALARM_OFF_REQ : ALARM_ON_REQ;
                    var alarmOnText = uiRes.AlarmOn ? "Turn Off" : "Turn On";
                    var sector0Val = uiRes.Sector0BoolValue ? "1" : "0";

                    text = text.Replace("{TAH}", alarmOnHref);
                    text = text.Replace("{TAT}", alarmOnText);
                    text = text.Replace("{S0V}", sector0Val);
                }
            }

            MemoryStream responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
            response.content = responseStream;

            return response;
        }
    }
}