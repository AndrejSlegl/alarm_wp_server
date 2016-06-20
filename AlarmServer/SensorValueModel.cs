using System;

namespace AlarmServer
{
    public class SensorValueModel : ModelBase
    {
        long v = 0;
        long min = long.MaxValue;
        long max = long.MinValue;
        bool boolValue;
        string value = "-";
        string minValue = "-";
        string maxValue = "-";
        UIColorValue uiColor = UIColorValue.None;
        readonly bool triggerAlarmValue;
        readonly Action<SensorValueModel> triggerAlarmAction;

        public string ParameterName { get; }
        public string Value { get { return value; } private set { if (value == this.value) return; this.value = value; RaisePropertyChanged(nameof(Value)); UpdateUIColorValue(); } }
        public string MinValue { get { return minValue; } private set { if (minValue == value) return; minValue = value; RaisePropertyChanged(nameof(MinValue)); } }
        public string MaxValue { get { return maxValue; } private set { if (maxValue == value) return; maxValue = value; RaisePropertyChanged(nameof(MaxValue)); } }

        public bool DisableUIColor { get; set; }

        public long LongValue { get { return v; } }

        public bool BoolValue
        {
            get { return boolValue; }
            private set
            {
                if (boolValue == value)
                    return;

                boolValue = value;
                RaisePropertyChanged(nameof(BoolValue));
            }
        }

        public UIColorValue UIColor { get { return uiColor; } private set { if (uiColor == value) return; uiColor = value; RaisePropertyChanged(nameof(UIColor)); } }

        public SensorValueModel(string parameterName, bool triggerAlarmValue = true, Action<SensorValueModel> triggerAlarmAction = null)
        {
            this.ParameterName = parameterName;
            this.triggerAlarmValue = triggerAlarmValue;
            this.triggerAlarmAction = triggerAlarmAction;
        }

        public void Update(long value)
        {
            v = value;
            Value = value.ToString();

            bool boolValue = DefaultBoolValueSetter(value);

            if (BoolValue != boolValue)
            {
                BoolValue = boolValue;

                if (boolValue == triggerAlarmValue && triggerAlarmAction != null)
                {
                    triggerAlarmAction(this);
                }
            }

            if (value < min)
            {
                min = value;
                MinValue = value.ToString();
            }
            if (value > max)
            {
                max = value;
                MaxValue = value.ToString();
            }

            UpdateUIColorValue();
        }

        public void Reset()
        {
            Value = "-";
            MinValue = "-";
            MaxValue = "-";
            BoolValue = false;

            v = 0;
            min = int.MaxValue;
            max = int.MinValue;

            UpdateUIColorValue();
        }

        public void Increment()
        {
            Update(v + 1);
        }

        void UpdateUIColorValue()
        {
            if (value == "-" || DisableUIColor)
            {
                UIColor = UIColorValue.None;
                return;
            }
            
            UIColor = boolValue == triggerAlarmValue ? UIColorValue.Red : UIColorValue.Green;
        }

        static bool DefaultBoolValueSetter(long val)
        {
            return val != 0;
        }
    }
}
