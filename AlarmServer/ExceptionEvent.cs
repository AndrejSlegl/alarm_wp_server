using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServer
{
    public class ExceptionEvent : EventModel
    {
        public ExceptionEvent(DateTime timestamp, Exception exception) : 
            base(timestamp, exception.Message, EventType.Exception)
        {
        }
    }
}
