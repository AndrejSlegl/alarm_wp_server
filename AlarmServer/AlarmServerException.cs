using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServer
{
    public class AlarmServerException : Exception
    {
        public AlarmServerException(string message) : 
            base(message)
        {

        }
    }
}
