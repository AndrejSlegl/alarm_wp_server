using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace AlarmServer
{
    class IOTClient : IIOTClient
    {
        public event ClientErrorHandler Disconnected;
        public event ClientMessageHandler MessageReceived;
        public event ClientErrorHandler ErrorOccured;

        public HostName RemoteAddress { get { return Socket.Information.RemoteAddress; } }

        internal StreamSocket Socket { get; }
        internal StreamWriter Writer { get; }
        internal StreamReader Reader { get; }

        internal IOTClient(StreamSocket socket, StreamReader reader, StreamWriter writer)
        {
            Socket = socket;
            Writer = writer;
            Reader = reader;
        }

        public async Task SendMessageAsync(IIOTMessage message)
        {
            StringBuilder builder = new StringBuilder();

            if (message.LongParameters != null)
            {
                foreach (var parameter in message.LongParameters)
                {
                    builder.Append(parameter.Key);
                    builder.Append(':');
                    builder.AppendLine(parameter.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (message.Flags != null)
            {
                foreach (var flag in message.Flags)
                {
                    builder.AppendLine(flag);
                }
            }

            await Writer.WriteAsync(builder.ToString());
            await Writer.FlushAsync();
        }

        internal async Task MessageLoop()
        {
            while (true)
            {
                IIOTMessage message = null;

                try
                {
                    message = await ParseRequestAsync();
                }
                catch (Exception ex)
                {
                    if (ex is AlarmServerException)
                        ErrorOccured?.Invoke(this, ex);
                    else
                    {
                        Disconnected?.Invoke(this, ex);
                        break;
                    }
                }

                if (message != null)
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
        }

        async Task<IIOTMessage> ParseRequestAsync()
        {
            string requestString = await Reader.ReadLineAsync();

            if (requestString == null)
                throw new Exception("Connection closed");

            var parametersSplit = requestString.Split(',');

            var longParameters = new Dictionary<string, long>(parametersSplit.Length);

            foreach (var item in parametersSplit)
            {
                var line = item.Trim(' ', '\r', '\t');
                var index = line.IndexOf(':');
                var lastIdx = line.Length - 1;

                if (index < 1 || index >= lastIdx)
                    continue;

                var parameterName = line.Substring(0, index);
                index++;
                var parameterValue = line.Substring(index, line.Length - index).Trim(' ', '\r', '\t');
                var value = long.Parse(parameterValue);

                longParameters.Add(parameterName, value);
            }

            if (longParameters.Count == 0)
                throw new AlarmServerException("no parameters found...");

            return new IOTMessage(longParameters);
        }

        public void Dispose()
        {
            Writer.Dispose();
            Reader.Dispose();
            Socket.Dispose();
        }
    }
}
