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
    //method to be called when url rule is met
    public delegate Task<WebResponse> RuleDeletage(WebResponse request);

    //delegate type for the error event
    public delegate void ErrorOccured(int code, string message);

    public class WebResponse
    {
        public string responseStatus;
        public IReadOnlyDictionary<string, string> header;
        public string method;
        public string uri;
        public Stream content;
    }

    public class WebServer
    {
        /// <summary>
        /// indicates if server should be listening
        /// </summary>
        protected bool isListening = false;
        protected string port;
        protected bool started;
        private readonly RuleDeletage requestHandler;

        /// <summary>
        /// event fired when an error occured
        /// </summary>
        public event ErrorOccured errorOccured;

        /// <summary>
        /// socket listener - the main IO part
        /// </summary>
        StreamSocketListener listener = new StreamSocketListener();
        
        public WebServer(RuleDeletage requestHandler, string port)
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
                started = false;

                //if possible fire the error event with the exception message
                errorOccured?.Invoke(-1, ex.Message);
            }
        }

        void listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            //if we should not be listening anymore, yet for some reason request was still parsed (phone not yet closed the socket) exit the method as it may be unwanted by the user for anybody to read any data
            if (isListening == false)
            {
                return;
            }

            //get the request socket
            StreamSocket sck = args.Socket;

            //create new task
            Task.Run(async () =>
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
                        responseStatus = "400 Bad Request",
                        header = new Dictionary<string, string>
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
                            responseStatus = "500 Internal Server Error",
                            header = new Dictionary<string, string>
                            {
                                { "Pragma", "no-cache" }
                            }
                        };
                    }
                }

                try
                {
                    await SendResponse(response, sck.OutputStream);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);

                    if (response.content != null)
                        response.content.Dispose();

                    sck.Dispose();
                }
            });
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
                throw new Exception("Invalid request");

            //create new request object (same type as response object - possibly better class naming needs to be used)
            WebResponse request = new WebResponse();

            //get the method (currently GET only supported really) and the request URL
            if (line.Substring(0, 3) == "GET")
            {
                request.method = "GET";
                request.uri = line.Substring(4);
            }
            else if (line.Substring(0, 4) == "POST")
            {
                request.method = "POST";
                request.uri = line.Substring(5);
            }

            //remove the HTTP version
            request.uri = Regex.Replace(request.uri, " HTTP.*$", "");

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
            request.header = headers;

            return request;
        }

        WebResponse ParseRequest(IInputStream inputStream)
        {
            var stream = inputStream.AsStreamForRead();
            var request = ParseHeader(stream);
            var contentStream = new MemoryStream();

            string contentLengthString = null;
            request.header.TryGetValue("Content-Length", out contentLengthString);

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
            
            request.content = contentStream;
            return request;
        }

        async Task SendResponse(WebResponse toSend, IOutputStream outputStream)
        {
            DataWriter writ = new DataWriter(outputStream);

            var status = toSend.responseStatus;

            if (status == null)
            {
                //if the rule is meant to redirect...
                if (toSend.header.ContainsKey("Location"))
                    status = "302";
                else
                    status = "200 OK";
            }

            writ.WriteString(CreateStatusString(status) + "\r\n");

            if (toSend.content != null)
            {
                //write content length to the buffer
                writ.WriteString("Content-Length: " + toSend.content.Length + "\r\n");
            }
            else
            {
                writ.WriteString("Content-Length: 0\r\n");
            }

            //for each of the response headers (returned by the delegate assigned to the URL rule
            foreach (string key in toSend.header.Keys)
            {
                //write it to the output
                writ.WriteString(key + ": " + toSend.header[key] + "\r\n");
            }

            //add connection: close header
            writ.WriteString("Connection: close\r\n");

            //new line before writing content
            writ.WriteString("\r\n");

            await writ.StoreAsync(); //wait for the data to be saved in the output
            await writ.FlushAsync(); //flush (send to the output)

            if (toSend.content != null)
            {
                //reset the output stream
                toSend.content.Seek(0, SeekOrigin.Begin);

                //write the data to the output using 1024 buffer (store and flush after every loop)
                while (toSend.content.Position < toSend.content.Length)
                {
                    byte[] buffer;
                    if (toSend.content.Length - toSend.content.Position < 1024)
                    {
                        buffer = new byte[toSend.content.Length - toSend.content.Position];
                    }
                    else
                    {
                        buffer = new byte[1024];
                    }
                    toSend.content.Read(buffer, 0, buffer.Length);
                    writ.WriteBytes(buffer);

                    await writ.StoreAsync();
                    await writ.FlushAsync();
                }
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
                responseStatus = "404 Not Found",
                header = headers,
                content = new MemoryStream(data)
            };
        }
    }
}
