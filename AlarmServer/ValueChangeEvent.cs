using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServer
{
    public class ValueChangeEvent : EventModel
    {
        public int Value { get; }
        public string Name { get; }

        public ValueChangeEvent(DateTime timestamp, string name, int value) : 
            base(timestamp, name + " " + ToString(value))
        {
            Name = name;
            Value = value;
        }

        static string ToString(int value)
        {
            return value.ToString();
        }
    }
}
