using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Core;
using System.Text;

namespace AlarmServer
{
    public class MainViewModel : ModelBase
    {
        class IOTClientUIInfo
        {
            public DateTime LastResponseTime { get; set; }
        }

        const string statusQuery = "status?";
        const string sirenOnText = "sirenOn";

        readonly Uri alarmSoundUri = new Uri("ms-appx:///Audio/Tornado_Siren_II-Delilah-747233690.mp3");
        readonly DispatcherTimer stopAlarmTimer = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(10) };
        readonly IIOTServer iotServer;
        readonly CoreDispatcher dispatcher;
        readonly IAudioPlayer audioPlayer;
        readonly Dictionary<IIOTClient, IOTClientUIInfo> clients = new Dictionary<IIOTClient, IOTClientUIInfo>();
        readonly DispatcherTimer timer = new DispatcherTimer();
        int clientPingInterval = 60;
        bool isConnected;
        bool isAlarmEnabled = true;
        bool isAlarmActive;

        public ObservableCollection<EventModel> Events { get; }

        public ObservableCollection<EventModel> AlarmTriggerEvents { get; }

        public List<SensorValueModel> Parameters { get; }

        public SensorValueModel Rssi { get; }
        public SensorValueModel Sector0 { get; }
        public SensorValueModel Movement0 { get; }
        public SensorValueModel DisconnectCount { get; }

        public bool IsConnected
        {
            get { return isConnected; }
            private set
            {
                if (isConnected == value)
                    return;

                isConnected = value;
                RaisePropertyChanged(nameof(IsConnected));
                StatusQueryCommand.IsEnabled = isConnected;
                CloseAllConnectionsCommand.IsEnabled = isConnected;
                ToggleSirenCommand.IsEnabled = isConnected;
            }
        }

        public bool IsAlarmActive
        {
            get { return isAlarmActive; }
            private set
            {
                if (isAlarmActive == value)
                    return;

                isAlarmActive = value;
                RaisePropertyChanged(nameof(IsAlarmActive));
                StopAlarmCommand.IsEnabled = isAlarmActive;
            }
        }

        public bool IsAlarmEnabled
        {
            get { return isAlarmEnabled; }
            private set
            {
                if (isAlarmEnabled == value)
                    return;

                isAlarmEnabled = value;
                RaisePropertyChanged(nameof(IsAlarmEnabled));
                RaisePropertyChanged(nameof(ToggleAlarmText));
            }
        }

        public string ToggleAlarmText { get { return IsAlarmEnabled ? "UGASNI" : "VKLOPI"; } }

        public UICommand StopAlarmCommand { get; }
        public UICommand AlarmToggleCommand { get; }
        public UICommand StatusQueryCommand { get; }
        public UICommand CloseAllConnectionsCommand { get; }
        public UICommand ToggleSirenCommand { get; }

        public MainViewModel(CoreDispatcher dispatcher, IIOTServer iotServer, IAudioPlayer audioPlayer)
        {
            this.iotServer = iotServer;
            this.audioPlayer = audioPlayer;
            this.dispatcher = dispatcher;

            Events = new ObservableCollection<EventModel>();
            AlarmTriggerEvents = new ObservableCollection<EventModel>();

            Rssi = new SensorValueModel("rssi");
            Rssi.DisableUIColor = true;
            DisconnectCount = new SensorValueModel("disconnect count");
            DisconnectCount.DisableUIColor = true;
            DisconnectCount.Update(0);

            Sector0 = new SensorValueModel("sector0", false, TriggerAlarm);
            Movement0 = new SensorValueModel("movement0", true, TriggerAlarm);

            Parameters = new List<SensorValueModel>
            {
                Rssi,
                Sector0,
                Movement0
            };

            StopAlarmCommand = new UICommand(StopAlarmSound, false);
            AlarmToggleCommand = new UICommand(AlarmToggleAction, true);
            StatusQueryCommand = new UICommand(QueryAllClientsStatus, false);
            CloseAllConnectionsCommand = new UICommand(CloseAllConnections, false);
            ToggleSirenCommand = new UICommand(ToggleSiren, false);
        }

        public async void StartIOTServer()
        {
            iotServer.ClientConnected -= IotServer_ClientConnected;
            iotServer.ClientConnected += IotServer_ClientConnected;

            try
            {
                await iotServer.StartServerAsync();
            }
            catch (Exception ex)
            {
                AddEvent(ex);
                return;
            }

            timer.Interval = TimeSpan.FromSeconds(clientPingInterval);
            timer.Tick += Timer_Tick;
            timer.Start();

            AddEvent("Server started");
        }

        private void IotServer_ClientConnected(IIOTServer server, IIOTClient client)
        {
            client.MessageReceived += Client_MessageReceived;
            client.ErrorOccured += Client_ErrorOccured;
            client.Disconnected += Client_Disconnected;

            Dispatch(() =>
            {
                clients.Add(client, new IOTClientUIInfo() { LastResponseTime = DateTime.Now });
                SendMessageSafe(client, new IOTMessage(CreateSirenOnParameter(), new string[] { statusQuery }));

                IsConnected = clients.Count > 0;

                AddEvent("New Connection " + client.RemoteAddress.ToString(), EventType.NewConnection);
            });
        }

        private void Client_Disconnected(IIOTClient client, Exception error)
        {
            client.MessageReceived -= Client_MessageReceived;
            client.ErrorOccured -= Client_ErrorOccured;
            client.Disconnected -= Client_Disconnected;

            Dispatch(() =>
            {
                clients.Remove(client);
                IsConnected = clients.Count > 0;

                if (error != null)
                    AddEvent(error);

                if (!IsConnected)
                {
                    DisconnectCount.Increment();

                    foreach (var parameter in Parameters)
                        parameter.Reset();
                }
            });
        }

        private void Client_ErrorOccured(IIOTClient client, Exception error)
        {
            Dispatch(() => AddEvent(error));
        }

        private void Client_MessageReceived(IIOTClient client, IIOTMessage message)
        {
            Dispatch(() =>
            {
                var data = clients[client];
                data.LastResponseTime = DateTime.Now;
                AddIOTMessage(message);
            });
        }

        async void SendMessageSafe(IIOTClient client, IIOTMessage message)
        {
            try
            {
                await client.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                AddEvent(ex);
            }
        }

        void SendMessageToAllSafe(IIOTMessage message)
        {
            foreach (var client in clients.Keys)
            {
                SendMessageSafe(client, message);
            }
        }

        async void Dispatch(DispatchedHandler agileCallback)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, agileCallback);
        }

        void Timer_Tick(object sender, object e)
        {
            foreach (var entry in clients)
            {
                if ((DateTime.Now - entry.Value.LastResponseTime).TotalSeconds > clientPingInterval * 2)
                {
                    entry.Key.Dispose(); // close connection if no response received for some time
                }
                else
                {
                    SendMessageSafe(entry.Key, new IOTMessage(new string[] { statusQuery }));
                }
            }
        }

        void StopAlarmTimer_Tick(object sender, object e)
        {
            StopAlarmSound();
        }

        void QueryAllClientsStatus()
        {
            SendMessageToAllSafe(new IOTMessage(new string[] { statusQuery }));
        }

        public void AddEvent(string text)
        {
            AddEvent(new EventModel(DateTime.Now, text));
        }

        public void AddEvent(string text, EventType type)
        {
            AddEvent(new EventModel(DateTime.Now, text, type));
        }

        public void AddEvent(Exception ex)
        {
            AddEvent(new ExceptionEvent(DateTime.Now, ex));
        }

        public void AddEvent(EventModel eventModel)
        {
            Events.Insert(0, eventModel);

            if (Events.Count > 200)
                Events.RemoveAt(Events.Count - 1);
        }

        void AddAlarmTriggerEvent(EventModel eventModel)
        {
            AlarmTriggerEvents.Insert(0, eventModel);

            if (AlarmTriggerEvents.Count > 100)
                AlarmTriggerEvents.RemoveAt(AlarmTriggerEvents.Count - 1);
        }

        void UpdateParameter(KeyValuePair<string, long> messageParameter)
        {
            var parameter = GetParameter(messageParameter.Key);

            if (parameter != null)
                parameter.Update(messageParameter.Value);
        }

        void AddIOTMessage(IIOTMessage message)
        {
            var builder = new StringBuilder();

            foreach (var parameter in message.LongParameters)
            {
                UpdateParameter(parameter);

                builder.Append(parameter.Key);
                builder.Append(": ");
                builder.Append(parameter.Value);
                builder.Append(", ");
            }

            AddEvent(new EventModel(DateTime.Now, builder.ToString().Trim(',', ' ')));
        }

        void StopAlarmSound()
        {
            if (!IsAlarmActive)
                return;

            stopAlarmTimer.Tick -= StopAlarmTimer_Tick;
            stopAlarmTimer.Stop();

            audioPlayer.Stop();
            IsAlarmActive = false;
            SendMessageToAllSafe(new IOTMessage(CreateSirenOnParameter()));
        }

        void ToggleSiren()
        {
            IsAlarmActive = !IsAlarmActive;
            SendMessageToAllSafe(new IOTMessage(CreateSirenOnParameter()));
        }

        void AlarmToggleAction()
        {
            IsAlarmEnabled = !IsAlarmEnabled;
        }

        void TriggerAlarm(SensorValueModel sensorValue)
        {
            StartAlarmSound();

            AddAlarmTriggerEvent(new EventModel(DateTime.Now, sensorValue.ParameterName));
        }

        void StartAlarmSound()
        {
            if (!IsAlarmEnabled)
                return;

            if (!IsAlarmActive)
            {
                stopAlarmTimer.Tick -= StopAlarmTimer_Tick;
                stopAlarmTimer.Tick += StopAlarmTimer_Tick;
                stopAlarmTimer.Start();
                audioPlayer.Play(alarmSoundUri, true);
                IsAlarmActive = true;
                SendMessageToAllSafe(new IOTMessage(CreateSirenOnParameter()));
            }
            else
            {
                stopAlarmTimer.Stop();
                stopAlarmTimer.Start();
            }
        }

        void CloseAllConnections()
        {
            foreach (var client in clients.Keys)
                client.Dispose();
        }

        public UIWebResponse HandleWebRequest(UIWebRequest request)
        {
            if(request.AlarmOn != null)
            {
                IsAlarmEnabled = request.AlarmOn.Value;
            }

            if (request.SirenOn != null)
            {
                if (request.SirenOn.Value)
                    System.Diagnostics.Debug.WriteLine("UIWebRequest.SirenOn");
                else
                    StopAlarmSound();
            }

            return new UIWebResponse()
            {
                AlarmOn = IsAlarmEnabled,
                Sector0Value = Sector0.LongValue,
                Movement0Value = Movement0.LongValue,
                RssiValue = Rssi.LongValue,
                SirenOn = IsAlarmActive,
                ClientsConnected = clients.Count
            };
        }

        SensorValueModel GetParameter(string parameterName)
        {
            return Parameters.FirstOrDefault(p => p.ParameterName == parameterName);
        }

        Dictionary<string, long> CreateSirenOnParameter()
        {
            return new Dictionary<string, long> { { sirenOnText, IsAlarmActive ? 1 : 0 } };
        }
    }
}
