using System.Collections.Generic;

namespace AlarmServer
{
    public interface IIOTMessage
    {
        IReadOnlyList<string> Flags { get; }
        IReadOnlyDictionary<string, long> LongParameters { get; }
    }
}
