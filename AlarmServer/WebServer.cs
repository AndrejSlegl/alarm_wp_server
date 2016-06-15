using System;
using System.Collections.Generic;
using System.IO;
using Windows.Networking.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Diagnostics;
using System.Text;

namespace AlarmServer
{
    public delegate Task<WebResponse> WebRequestHandlerDelegate(WebResponse request);

    public class WebResponse
    {
        public string ResponseStatus { get; set; }
        public IReadOnlyDictionary<string, string> Headers { get; set; }
        public string Method { get; set; }
        public string Uri { get; set; }
        public Stream Content { get; set; }
    }

    public class WebServer
    {
        readonly string port;
        readonly WebRequestHandlerDelegate requestHandler;
        bool started;
        bool isListening = false;

        readonly StreamSocketListener listener = new StreamSocketListener();
        
        public WebServer(WebRequestHandlerDelegate requestHandler, string port)
        {
            System.Diagnostics.Contracts.Contract.Requires(requestHandler != null);

            this.requestHandler = requestHandler;
            this.port = port;
        }

        public async void StartServer()
        {
            if (started)
                return;

            started = true;

            try
            {
                listener.ConnectionReceived += listener_ConnectionReceived;
                await listener.BindServiceNameAsync(port);

                isListening = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                started = false;
            }
        }

        void listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            if (isListening == false)
            {
                return;
            }
            
            StreamSocket sck = args.Socket;
            
            Task.Run(() => HandleNewConnectionAsync(sck));
        }

        async Task HandleNewConnectionAsync(StreamSocket sck)
        {
            WebResponse request = null;
            WebResponse response = null;

            try
            {
                request = ParseRequest(sck.InputStream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                response = new WebResponse
                {
                    ResponseStatus = "400 Bad Request",
                    Headers = new Dictionary<string, string>
                    {
                        { "Pragma", "no-cache" }
                    }
                };
            }

            if (response == null)
            {
                try
                {
                    response = await requestHandler(request);

                    if (response == null)
                        response = CreateNotFoundResponse();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);

                    response = new WebResponse
                    {
                        ResponseStatus = "500 Internal Server Error",
                        Headers = new Dictionary<string, string>
                        {
                            { "Pragma", "no-cache" }
                        }
                    };
                }
            }

            try
            {
                SendResponse(response, sck.OutputStream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                sck.Dispose();
            }
            finally
            {
                if (response.Content != null)
                    response.Content.Dispose();
            }
        }

        WebResponse ParseHeader(Stream stream)
        {
            byte[] buffer = new byte[4096];
            int bufferIndex = 0;
            bool isPrevNewLine = false;

            while (true)
            {
                int val = stream.ReadByte();

                if (val == -1)
                    break;

                byte b = Convert.ToByte(val);
                char c = Convert.ToChar(b);

                if (bufferIndex >= buffer.Length)
                    throw new Exception("Request header too large");

                buffer[bufferIndex++] = b;

                if (c == '\n')
                {
                    if (isPrevNewLine)
                        break;

                    isPrevNewLine = true;
                }
                else if (c != '\r')
                {
                    isPrevNewLine = false;
                }
            }

            string headersString = Encoding.UTF8.GetString(buffer, 0, bufferIndex);
            var lines = headersString.Split('\n');
            var line = lines[0].Trim('\r');

            if (string.IsNullOrEmpty(line))
                throw new Exception("Request is empty");

            //create new request object (same type as response object - possibly better class naming needs to be used)
            WebResponse request = new WebResponse();

            //get the method (currently GET only supported really) and the request URL
            if (line.Substring(0, 3) == "GET")
            {
                request.Method = "GET";
                request.Uri = line.Substring(4);
            }
            else if (line.Substring(0, 4) == "POST")
            {
                request.Method = "POST";
                request.Uri = line.Substring(5);
            }

            if (request.Method == null)
                throw new Exception("Unsupported HTTP method");

            //remove the HTTP version
            request.Uri = Regex.Replace(request.Uri, " HTTP.*$", "");

            //create a dictionary for the sent headers
            Dictionary<string, string> headers = new Dictionary<string, string>();
            string[] sepa = new string[] { ":" };

            for (int i = 1; i < lines.Length; ++i)
            {
                line = lines[i].Trim('\r');

                if (string.IsNullOrEmpty(line))
                    continue;

                string[] elems = line.Split(sepa, 2, StringSplitOptions.RemoveEmptyEntries);

                if (elems.Length > 1)
                    headers.Add(elems[0], elems[1]);
            }

            //assign headers to the request object
            request.Headers = headers;

            return request;
        }

        WebResponse ParseRequest(IInputStream inputStream)
        {
            var stream = inputStream.AsStreamForRead();
            var request = ParseHeader(stream);
            var contentStream = new MemoryStream();

            string contentLengthString = null;
            request.Headers.TryGetValue("Content-Length", out contentLengthString);

            if (!string.IsNullOrEmpty(contentLengthString))
            {
                byte[] contentBuffer = new byte[2048];
                int contentLength = int.Parse(contentLengthString);
                int bytesRead = 0;

                while (bytesRead < contentLength)
                {
                    int readCount = contentLength - bytesRead;

                    if (readCount > contentBuffer.Length)
                        readCount = contentBuffer.Length;

                    int count = stream.Read(contentBuffer, 0, readCount);
                    bytesRead += count;
                    contentStream.Write(contentBuffer, 0, count);
                }

                contentStream.Seek(0, SeekOrigin.Begin);
            }
            
            request.Content = contentStream;
            return request;
        }

        void SendResponse(WebResponse response, IOutputStream outputStream)
        {
            Stream stream = outputStream.AsStreamForWrite();
            StreamWriter writer = new StreamWriter(stream);

            var status = response.ResponseStatus;
            long contentLength = response.Content == null ? 0 : response.Content.Length;

            if (status == null)
            {
                if (response.Headers != null && response.Headers.ContainsKey("Location"))
                    status = "302";
                else
                    status = "200 OK";
            }

            writer.WriteLine(CreateStatusString(status));
            writer.WriteLine("Content-Length: " + contentLength);

            if (response.Headers != null)
            {
                foreach (var pair in response.Headers)
                {
                    writer.Write(pair.Key);
                    writer.Write(": ");
                    writer.WriteLine(pair.Value);
                }
            }
            
            writer.WriteLine("Connection: close");
            writer.WriteLine();
            writer.Flush();

            if (response.Content == null)
                return;
            
            response.Content.Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[2048];
            int bytesRead = 0;
            
            while (bytesRead < contentLength)
            {
                int count = response.Content.Read(buffer, 0, buffer.Length);
                bytesRead += count;
                
                stream.Write(buffer, 0, count);
                stream.Flush();
            }
        }

        string CreateStatusString(string status)
        {
            return "HTTP/1.1 " + status;
        }

        WebResponse CreateNotFoundResponse()
        {
            var headers = new Dictionary<string, string>()
            {
                { "Content-Type", "text/html" },
                { "Pragma", "no-cache" },
            };
            
            var data = Encoding.UTF8.GetBytes("Not found");

            return new WebResponse()
            {
                ResponseStatus = "404 Not Found",
                Headers = headers,
                Content = new MemoryStream(data)
            };
        }
    }
}
