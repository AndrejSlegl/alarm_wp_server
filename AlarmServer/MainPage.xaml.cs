﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Phone.UI.Input;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace AlarmServer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string port = "43254";
        const string statusQuery = "status?";
        const string stopSirenText = "stopSiren!";
        const string sirenActivatedText = "sirenActivated";

        readonly DispatcherTimer timer = new DispatcherTimer();
        readonly MainViewModel model;
        readonly List<StreamSocket> openedSockets = new List<StreamSocket>();
        StreamSocketListener listener;

        readonly List<StreamWriter> streamWriters = new List<StreamWriter>();
        Popup openedPopup;
        int clientPingInterval = 60;
        DateTime? lastResponseTime = null;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            model = (Application.Current as App).MainModel;
            DataContext = model;

            timer.Interval = TimeSpan.FromSeconds(clientPingInterval);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            if (lastResponseTime.HasValue && (DateTime.Now - lastResponseTime.Value).TotalSeconds > clientPingInterval * 2)
            {
                CloseAllOpenedConnections();
            }
            else
            {
                QueryClientStatus();
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            StartServer();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;

            base.OnNavigatedFrom(e);
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = CloseOpenedPopup();
        }

        bool CloseOpenedPopup()
        {
            if (openedPopup != null && openedPopup.IsOpen)
            {
                openedPopup.IsOpen = false;
                openedPopup = null;
                return true;
            }

            return false;
        }

        async void StartServer()
        {
            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }

            if (listener == null)
            {
                listener = new StreamSocketListener();
                listener.ConnectionReceived += Listener_ConnectionReceived;
            }
            
            try
            {
                await listener.BindServiceNameAsync(port);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                model.AddEvent(ex);
                return;
            }

            model.AddEvent("Server started");
        }

        async void Dispatch(DispatchedHandler agileCallback)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, agileCallback);
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            ClientMessage clientMessage = null;
            Exception error = null;
            bool success = false;
            var socket = args.Socket;

            lock (openedSockets)
            {
                var existingSocket = openedSockets.FirstOrDefault(s => s.Information.RemoteAddress.Equals(socket.Information.RemoteAddress));
                if (existingSocket != null)
                {
                    existingSocket.Dispose();
                    openedSockets.Remove(existingSocket);
                }

                openedSockets.Add(socket);
            }

            var streamReader = new StreamReader(socket.InputStream.AsStreamForRead());
            var streamWriter = new StreamWriter(socket.OutputStream.AsStreamForWrite());

            var remoteAddress = socket.Information.RemoteAddress;
            string remoteIp = remoteAddress.ToString();

            Dispatch(() =>
            {
                lastResponseTime = DateTime.Now;
                model.AddEvent("New Connection " + remoteIp, EventType.NewConnection);
                model.ClientConnected();
            });
            
            StringBuilder builder = new StringBuilder();
            builder.Append(sirenActivatedText);
            builder.Append(':');
            builder.AppendLine(model.IsAlarmEnabled ? "1" : "0");
            builder.AppendLine(statusQuery);

            try
            {
                await SendMessageAsync(streamWriter, builder.ToString());
                success = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Dispatch(() => model.AddEvent(ex));
            }

            if (success)
            {
                Dispatch(() => streamWriters.Add(streamWriter));

                while (true)
                {
                    try
                    {
                        clientMessage = await ParseRequestAsync(streamReader);
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                        Debug.WriteLine(ex);
                        Dispatch(() => model.AddEvent(ex));

                        if (!(ex is AlarmServerException))
                            break;
                    }

                    if (clientMessage != null)
                    {
                        Dispatch(() =>
                        {
                            lastResponseTime = DateTime.Now;
                            model.AddClientMessage(clientMessage);
                        });
                    }
                }
            }

            streamReader.Dispose();
            streamWriter.Dispose();
            socket.Dispose();

            lock (openedSockets)
            {
                openedSockets.Remove(socket);
            }

            Dispatch(() => 
            {
                streamWriters.Remove(streamWriter);
                
                model.ClientDisconnected();
            });
        }

        async Task<ClientMessage> ParseRequestAsync(StreamReader streamReader)
        {
            string requestString = await streamReader.ReadLineAsync();

            if (requestString == null)
                throw new Exception("Connection closed");

            var parametersSplit = requestString.Split(',');

            List<ValueChangeEvent> valueChangeEvents = new List<ValueChangeEvent>(parametersSplit.Length);
            DateTime dt = DateTime.Now;

            foreach (var item in parametersSplit)
            {
                var line = item.Trim(' ', '\r', '\t');
                var index = line.IndexOf(':');
                var lastIdx = line.Length - 1;

                if (index < 1 || index >= lastIdx)
                    continue;

                string parameterName = line.Substring(0, index);
                index++;
                string parameterValue = line.Substring(index, line.Length - index).Trim(' ', '\r', '\t');
                int value = int.Parse(parameterValue);

                valueChangeEvents.Add(new ValueChangeEvent(dt, parameterName, value));
            }

            if (valueChangeEvents.Count == 0)
                throw new AlarmServerException("no parameters found...");

            return new ClientMessage(valueChangeEvents);
        }

        async Task SendMessageAsync(StreamWriter streamWriter, string message)
        {
            await streamWriter.WriteLineAsync(message);
            await streamWriter.FlushAsync();
        }

        async Task SendMessageSafeAsync(string message)
        {
            foreach (var streamWriter in streamWriters)
            {
                try
                {
                    await SendMessageAsync(streamWriter, message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Dispatch(() => model.AddEvent(ex));
                }
            }
        }

        public async void QueryClientStatus()
        {
            await SendMessageSafeAsync(statusQuery);
        }

        void CloseAllOpenedConnections()
        {
            lock (openedSockets)
            {
                foreach (var socket in openedSockets)
                {
                    socket.Dispose();
                }

                openedSockets.Clear();
            }
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            model.StopAlarmSound();
            await SendMessageSafeAsync(stopSirenText);
        }

        private void OpenConsoleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CloseOpenedPopup())
                return;

             ConsoleControl control = new ConsoleControl()
            {
                Width = ActualWidth,
                Height = ActualHeight,
                DataContext = model,
                MainPage = this
            };
            
            openedPopup = new Popup() { Child = control };
            openedPopup.IsOpen = true;
        }

        private async void AlarmToggleButton_Click(object sender, RoutedEventArgs e)
        {
            model.AlarmToggleButtonClick();

            string val = model.IsAlarmEnabled ? "1" : "0";
            await SendMessageSafeAsync(sirenActivatedText + ":" + val);
        }

        private void RestartServerBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseAllOpenedConnections();
        }
    }
}
