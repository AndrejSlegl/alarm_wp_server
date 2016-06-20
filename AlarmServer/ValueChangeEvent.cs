using System;

namespace AlarmServer
{
    public class ValueChangeEvent : EventModel
    {
        public long Value { get; }
        public string Name { get; }

        public ValueChangeEvent(DateTime timestamp, string name, long value) : 
            base(timestamp, name + " " + ToString(value))
        {
            Name = name;
            Value = value;
        }

        static string ToString(long value)
        {
            return value.ToString();
        }
    }
}
