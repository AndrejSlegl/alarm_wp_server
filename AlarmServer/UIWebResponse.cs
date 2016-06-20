using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServer
{
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

        public void Serialize(Stream stream)
        {
            serializer.WriteObject(stream, this);
        }
    }
}
