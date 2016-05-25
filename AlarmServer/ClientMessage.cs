using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServer
{
    public class ClientMessage
    {
        public IReadOnlyList<ValueChangeEvent> Events { get; }

        public ClientMessage(IReadOnlyList<ValueChangeEvent> events)
        {
            Events = events;
        }
    }
}
