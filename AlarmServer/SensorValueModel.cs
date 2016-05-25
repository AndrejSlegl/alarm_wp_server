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
        Func<int, bool> boolValueSetter;

        public string ParameterName { get; }
        public string Value { get { return value; } private set { if (value == this.value) return; this.value = value; RaisePropertyChanged(nameof(Value)); } }
        public string MinValue { get { return minValue; } private set { if (minValue == value) return; minValue = value; RaisePropertyChanged(nameof(MinValue)); } }
        public string MaxValue { get { return maxValue; } private set { if (maxValue == value) return; maxValue = value; RaisePropertyChanged(nameof(MaxValue)); } }

        public bool BoolValue { get { return boolValue; } private set { if (boolValue == value) return; boolValue = value; RaisePropertyChanged(nameof(BoolValue)); } }

        public SensorValueModel(string parameterName) : 
            this(parameterName, DefaultBoolValueSetter)
        {
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
            BoolValue = boolValueSetter(value);

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

        static bool DefaultBoolValueSetter(int val)
        {
            return val != 0;
        }
    }
}
