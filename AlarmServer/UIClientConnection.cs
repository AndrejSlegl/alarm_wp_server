using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace AlarmServer
{
    public class UIClientConnection : IDisposable
    {
        readonly StreamSocket socket;
        readonly string hostName;
        readonly string remoteServiceName;

        public UIClientConnection(string hostName, string remoteServiceName)
        {
            this.hostName = hostName;
            this.remoteServiceName = remoteServiceName;
            socket = new StreamSocket();
        }

        public void Dispose()
        {
            socket.Dispose();
        }

        public async Task SendMessageAsync(string message)
        {
            await socket.ConnectAsync(new HostName(hostName), remoteServiceName);
            DataWriter writer = null;

            try
            {
                writer = new DataWriter(socket.OutputStream);
                writer.WriteString(message);
                await writer.StoreAsync();
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }
        }
    }
}
