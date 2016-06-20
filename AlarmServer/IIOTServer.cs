using System;
using System.Threading.Tasks;

namespace AlarmServer
{
    public delegate void ClientConnectedHandler(IIOTServer server, IIOTClient client);

    public interface IIOTServer : IDisposable
    {
        event ClientConnectedHandler ClientConnected;

        Task StartServerAsync();
    }
}
