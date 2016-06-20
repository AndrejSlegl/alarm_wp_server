using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace AlarmServer
{
    public class IOTServer : IIOTServer
    {
        string port;
        readonly List<IOTClient> clientList = new List<IOTClient>();
        StreamSocketListener listener;

        public event ClientConnectedHandler ClientConnected;

        public IOTServer(string port)
        {
            this.port = port;
        }

        public async Task StartServerAsync()
        {
            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }

            if (listener == null)
            {
                listener = new StreamSocketListener();
                listener.ConnectionReceived += Listener_ConnectionReceived;
            }

            await listener.BindServiceNameAsync(port);
        }

        void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            Task.Run(() => HandleClientConnection(args.Socket));
        }

        async void HandleClientConnection(StreamSocket socket)
        {
            var client = new IOTClient(socket);

            lock (clientList)
            {
                var existingClient = clientList.FirstOrDefault(c => c.RemoteAddress.Equals(socket.Information.RemoteAddress));
                if (existingClient != null)
                {
                    existingClient.Dispose();
                    clientList.Remove(existingClient);
                }

                clientList.Add(client);
            }

            ClientConnected?.Invoke(this, client);

            await client.MessageLoop();

            lock (clientList)
            {
                clientList.Remove(client);
                client.Dispose();
            }
        }

        public void Dispose()
        {
            lock (clientList)
            {
                foreach (var client in clientList)
                {
                    client.Dispose();
                }
            }

            listener.Dispose();
        }
    }
}
