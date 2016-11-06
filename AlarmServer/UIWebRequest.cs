using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace AlarmServer
{
    [DataContract]
    public class UIWebRequest
    {
        readonly static DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(UIWebRequest));

        [DataMember(Name = "alarmOn", EmitDefaultValue = false)]
        public bool? AlarmOn { get; set; }

        [DataMember(Name = "sirenOn", EmitDefaultValue = false)]
        public bool? SirenOn { get; set; }

        [DataMember(Name = "sector0TriggerEnabled", EmitDefaultValue = false)]
        public bool? Sector0TriggerEnabled { get; set; }

        public static UIWebRequest Deserialize(Stream stream)
        {
            return (UIWebRequest)serializer.ReadObject(stream);
        }
    }
}
