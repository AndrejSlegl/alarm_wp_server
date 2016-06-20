using System.Collections.Generic;

namespace AlarmServer
{
    public class IOTMessage : IIOTMessage
    {
        public IReadOnlyDictionary<string, long> LongParameters { get; }

        public IReadOnlyList<string> Flags { get; }

        public IOTMessage(IReadOnlyDictionary<string, long> longParameters)
        {
            LongParameters = longParameters;
        }

        public IOTMessage(IReadOnlyList<string> flags)
        {
            Flags = flags;
        }

        public IOTMessage(IReadOnlyDictionary<string, long> longParameters, IReadOnlyList<string> flags)
        {
            LongParameters = longParameters;
            Flags = flags;
        }
    }
}
