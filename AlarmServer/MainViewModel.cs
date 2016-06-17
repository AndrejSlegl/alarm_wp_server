using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;

namespace AlarmServer
{
    public class MainViewModel : ModelBase
    {
        readonly Uri alarmSoundUri = new Uri("ms-appx:///Audio/Tornado_Siren_II-Delilah-747233690.mp3");
        readonly DispatcherTimer stopAlarmTimer = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(10) };
        int numConnections;
        bool isConnected;
        bool isAlarmEnabled = true;
        Uri alarmSoundSource;
        
        public ObservableCollection<EventModel> Events { get; }

        public ObservableCollection<EventModel> AlarmTriggerEvents { get; }

        public List<SensorValueModel> Parameters { get; }

        public SensorValueModel Rssi { get; }
        public SensorValueModel Sector0 { get; }
        public SensorValueModel Movement0 { get; }
        public SensorValueModel DisconnectCount { get; }

        public bool IsConnected { get { return isConnected; } private set { if (isConnected == value) return; isConnected = value; RaisePropertyChanged(nameof(IsConnected)); } }

        public Uri AlarmSoundSource { get { return alarmSoundSource; } private set { alarmSoundSource = value; RaisePropertyChanged(nameof(AlarmSoundSource)); } }

        public bool IsAlarmEnabled { get { return isAlarmEnabled; } set { if (isAlarmEnabled == value) return; isAlarmEnabled = value; RaisePropertyChanged(nameof(IsAlarmEnabled)); RaisePropertyChanged(nameof(ToggleAlarmText)); } }

        public string ToggleAlarmText { get { return IsAlarmEnabled ? "UGASNI" : "VKLOPI"; } }

        public MainViewModel()
        {
            Events = new ObservableCollection<EventModel>();
            AlarmTriggerEvents = new ObservableCollection<EventModel>();

            Rssi = new SensorValueModel("rssi");
            Sector0 = new SensorValueModel("sector0", false, TriggerAlarm);
            Movement0 = new SensorValueModel("movement0", true, TriggerAlarm);
            DisconnectCount = new SensorValueModel("disconnect count", val => false);
            DisconnectCount.Update(0);

            Parameters = new List<SensorValueModel>
            {
                Rssi,
                Sector0,
                Movement0
            };
        }

        private void StopAlarmTimer_Tick(object sender, object e)
        {
            StopAlarmSound();
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

            if (Events.Count > 100)
                Events.RemoveAt(Events.Count - 1);
        }

        void AddAlarmTriggerEvent(EventModel eventModel)
        {
            AlarmTriggerEvents.Insert(0, eventModel);

            if (AlarmTriggerEvents.Count > 100)
                AlarmTriggerEvents.RemoveAt(AlarmTriggerEvents.Count - 1);
        }

        public void AddEvent(ValueChangeEvent valueChangeEvent)
        {
            var parameter = GetParameter(valueChangeEvent.Name);

            bool sector0Value = Sector0.BoolValue;
            bool movement0Value = Movement0.BoolValue;

            if (parameter != null)
                parameter.Update(valueChangeEvent.Value);

            AddEvent((EventModel)valueChangeEvent);
        }

        public void AddClientMessage(ClientMessage clientMessage)
        {
            foreach(var ev in clientMessage.Events)
            {
                AddEvent(ev);
            }
        }

        public void ClientConnected()
        {
            if (numConnections == 0)
                IsConnected = true;

            numConnections++;
        }

        public void ClientDisconnected()
        {
            numConnections--;

            if (numConnections == 0)
            {
                IsConnected = false;
                DisconnectCount.Increment();
                Rssi.Reset();
            }
        }

        public void StopAlarmSound()
        {
            AlarmSoundSource = null;
            stopAlarmTimer.Tick -= StopAlarmTimer_Tick;
            stopAlarmTimer.Stop();
        }

        public void AlarmToggleButtonClick()
        {
            IsAlarmEnabled = !IsAlarmEnabled;
        }

        void TriggerAlarm(SensorValueModel sensorValue)
        {
            StartAlarmSound();

            AddAlarmTriggerEvent(new EventModel(DateTime.Now, sensorValue.ParameterName));
        }

        public void StartAlarmSound()
        {
            if (!IsAlarmEnabled)
                return;

            if (AlarmSoundSource == null)
            {
                stopAlarmTimer.Tick -= StopAlarmTimer_Tick;
                stopAlarmTimer.Tick += StopAlarmTimer_Tick;
                stopAlarmTimer.Start();
                AlarmSoundSource = alarmSoundUri;
            }
            else
            {
                stopAlarmTimer.Stop();
                stopAlarmTimer.Start();
            }
        }

        public UIWebResponse HandleWebRequest(UIWebRequest request)
        {
            if(request.AlarmOn != null)
            {
                IsAlarmEnabled = request.AlarmOn.Value;
            }

            return new UIWebResponse()
            {
                AlarmOn = IsAlarmEnabled,
                Sector0BoolValue = Sector0.BoolValue
            };
        }

        SensorValueModel GetParameter(string parameterName)
        {
            return Parameters.FirstOrDefault(p => p.ParameterName == parameterName);
        }
    }
}
