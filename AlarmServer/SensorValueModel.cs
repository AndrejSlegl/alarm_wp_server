﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServer
{
    public class SensorValueModel : ModelBase
    {
        int v = 0;
        int min = int.MaxValue;
        int max = int.MinValue;
        bool boolValue;
        string value = "-";
        string minValue = "-";
        string maxValue = "-";
        UIColorValue uiColor;
        readonly bool triggerAlarmValue;
        readonly Func<int, bool> boolValueSetter;
        readonly Action<SensorValueModel> triggerAlarmAction;

        public string ParameterName { get; }
        public string Value { get { return value; } private set { if (value == this.value) return; this.value = value; RaisePropertyChanged(nameof(Value)); UpdateUIColorValue(); } }
        public string MinValue { get { return minValue; } private set { if (minValue == value) return; minValue = value; RaisePropertyChanged(nameof(MinValue)); } }
        public string MaxValue { get { return maxValue; } private set { if (maxValue == value) return; maxValue = value; RaisePropertyChanged(nameof(MaxValue)); } }

        public bool BoolValue
        {
            get { return boolValue; }
            private set
            {
                if (boolValue == value)
                    return;

                boolValue = value;
                RaisePropertyChanged(nameof(BoolValue));
                UpdateUIColorValue();
            }
        }

        public UIColorValue UIColor { get { return uiColor; } private set { if (uiColor == value) return; uiColor = value; RaisePropertyChanged(nameof(UIColor)); } }

        public SensorValueModel(string parameterName, bool triggerAlarmValue = true, Action<SensorValueModel> triggerAlarmAction = null) : 
            this(parameterName, DefaultBoolValueSetter)
        {
            this.triggerAlarmValue = triggerAlarmValue;
            this.triggerAlarmAction = triggerAlarmAction;
        }

        public SensorValueModel(string parameterName, Func<int, bool> boolValueSetter)
        {
            ParameterName = parameterName;
            this.boolValueSetter = boolValueSetter;
        }

        public void Update(int value)
        {
            v = value;
            Value = value.ToString();

            bool boolValue = boolValueSetter(value);

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
        }

        public void Increment()
        {
            Update(v + 1);
        }

        void UpdateUIColorValue()
        {
            if (value == "-")
            {
                UIColor = UIColorValue.None;
                return;
            }
            
            UIColor = boolValue == triggerAlarmValue ? UIColorValue.Red : UIColorValue.Green;
        }

        static bool DefaultBoolValueSetter(int val)
        {
            return val != 0;
        }
    }
}
