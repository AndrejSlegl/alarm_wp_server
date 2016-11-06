using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace AlarmServer
{
    [DataContract]
    public class UIWebResponseEvent
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }

    [DataContract]
    public class UIWebResponse
    {
        readonly static DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(UIWebResponse));

        [DataMember(Name = "alarmOn")]
        public bool AlarmOn { get; set; }

        [DataMember(Name = "sector0Value")]
        public long Sector0Value { get; set; }

        [DataMember(Name = "movement0Value")]
        public long Movement0Value { get; set; }

        [DataMember(Name = "rssiValue")]
        public long RssiValue { get; set; }

        [DataMember(Name = "clientsConnected")]
        public long ClientsConnected { get; set; }

        [DataMember(Name = "sirenOn")]
        public bool SirenOn { get; set; }

        [DataMember(Name = "alarmTriggerEvents")]
        public List<UIWebResponseEvent> AlarmTriggerEvents { get; set; }

        public void Serialize(Stream stream)
        {
            serializer.WriteObject(stream, this);
        }
    }
}
