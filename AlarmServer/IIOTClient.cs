using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;

namespace AlarmServer
{
    public delegate void ClientMessageHandler(IIOTClient client, IIOTMessage message);
    public delegate void ClientErrorHandler(IIOTClient client, Exception error);

    public interface IIOTClient : IDisposable
    {
        event ClientErrorHandler Disconnected;
        event ClientMessageHandler MessageReceived;
        event ClientErrorHandler ErrorOccured;

        HostName RemoteAddress { get; }
        Task SendMessageAsync(IIOTMessage message);
    }
}
