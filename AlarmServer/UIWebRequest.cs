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
    public class UIWebRequest
    {
        readonly static DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(UIWebRequest));

        [DataMember(Name = "alarmOn", EmitDefaultValue = false)]
        public bool? AlarmOn { get; set; }

        public static UIWebRequest Deserialize(Stream stream)
        {
            return (UIWebRequest)serializer.ReadObject(stream);
        }
    }
}
