using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServer
{
    public class EventModel
    {
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }

        public string Description { get { return Timestamp.ToString("HH:mm:ss") + ": " + Text; } }

        public EventType Type { get; }

        public EventModel(DateTime timestamp, string text) : 
            this(timestamp, text, EventType.Message)
        {
        }

        public EventModel(DateTime timestamp, string text, EventType type)
        {
            Text = text;
            Timestamp = timestamp;
            Type = type;
        }
    }
}
