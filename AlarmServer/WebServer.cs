using System;
using System.Collections.Generic;
using System.IO;
using Windows.Networking.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace AlarmServer
{
    //method to be called when url rule is met
    public delegate Task<WebResponse> RuleDeletage(WebResponse response);

    //delegate type for the error event
    public delegate void ErrorOccured(int code, string message);

    public struct WebResponse
    {
        public Dictionary<string, string> header;
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

        /// <summary>
        /// event fired when an error occured
        /// </summary>
        public event ErrorOccured errorOccured;

        /// <summary>
        /// socket listener - the main IO part
        /// </summary>
        StreamSocketListener listener = new StreamSocketListener();

        /// <summary>
        /// rules: Url Regex => method to be called when rule is met
        /// </summary>
        private Dictionary<string, RuleDeletage> serverRules;

        /// <summary>
        /// initializes the new server object and puts it in listening mode
        /// </summary>
        /// <param name="rules">set of rules in a form "Url Regex" => "Method to be fired when rule is met"</param>
        /// <param name="ip">ip to bind to</param>
        /// <param name="port">port to bind to</param>
        public WebServer(Dictionary<string, RuleDeletage> rules, string port)
        {
            //assign passed rules to the server
            serverRules = rules;
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
                try
                {
                    var stream = sck.InputStream.AsStreamForRead();
                    await parseRequest(stream, sck);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex);
                    sck.Dispose();
                }
                
            });//Task
        }

        /// <summary>
        /// method which parses the request and decides about the action
        /// </summary>
        /// <param name="request_file">path to the temporary file holding the request packet</param>
        /// <param name="socket">socket used with the request (required for response)</param>
        public async Task parseRequest(Stream requestStream, StreamSocket socket)
        {
            StreamReader reada = new StreamReader(requestStream);

            //read the first ling to get request type
            string linge = reada.ReadLine();

            //create new request object (same type as response object - possibly better class naming needs to be used)
            WebResponse request = new WebResponse();

            //if there is any data...
            if (linge != null && linge.Length > 0)
            {
                //get the method (currently GET only supported really) and the request URL
                if (linge.Substring(0, 3) == "GET")
                {
                    request.method = "GET";
                    request.uri = linge.Substring(4);
                }
                else if (linge.Substring(0, 4) == "POST")
                {
                    request.method = "POST";
                    request.uri = linge.Substring(5);
                }

                //remove the HTTP version
                request.uri = Regex.Replace(request.uri, " HTTP.*$", "");

                //create a dictionary for the sent headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                //read HTTP headers into the dictionary
                string line = "";
                do
                {
                    line = reada.ReadLine();
                    string[] sepa = new string[1];
                    sepa[0] = ":";
                    string[] elems = line.Split(sepa, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (elems.Length > 0)
                    {
                        headers.Add(elems[0], elems[1]);
                    }
                } while (line.Length > 0);

                //assign headers to the request object
                request.header = headers;

                //assigne rest of the content to the request object in a form of a stream handle moved to the part with content after previous read line operations
                request.content = reada.BaseStream;

                //determines if we found a matching URL rule
                bool foundrule = false;

                //create a stream writer to the output stream (response)
                DataWriter writ = new DataWriter(socket.OutputStream);

                Debug.WriteLine("URL:" + request.uri);

                //if there are any server rules
                if (serverRules != null)
                {
                    //for every rule...
                    foreach (var rule in serverRules)
                    {
                        //if it matches the URL
                        if (request.uri.StartsWith(rule.Key))
                        {
                            //create a new response object
                            //assign to it response from the method called as a delegate assigned to the URL rule
                            WebResponse toSend = await rule.Value(request);

                            //mark that we found a rule
                            foundrule = true;

                            try
                            {
                                //if the rule is meant to redirect...
                                if (toSend.header.ContainsKey("Location"))
                                {
                                    writ.WriteString("HTTP/1.1 302\r\n");
                                }
                                else //if a normal read operation
                                {
                                    writ.WriteString("HTTP/1.1 200 OK\r\n");
                                }

                                //write content length to the buffer
                                writ.WriteString("Content-Length: " + toSend.content.Length + "\r\n");

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

                                //reset the output stream
                                toSend.content.Seek(0, SeekOrigin.Begin);

                                await writ.StoreAsync(); //wait for the data to be saved in the output
                                await writ.FlushAsync(); //flush (send to the output)

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
                            finally
                            {
                                toSend.content.Dispose();
                            }

                            break;
                        }
                    }
                }

                if (foundrule == false)
                {
                    writ.WriteString("HTTP/1.1 404 Not Found\r\n");
                    writ.WriteString("Content-Type: text/html\r\n");
                    writ.WriteString("Content-Length: 9\r\n");
                    writ.WriteString("Pragma: no-cache\r\n");
                    writ.WriteString("Connection: close\r\n");
                    writ.WriteString("\r\n");
                    writ.WriteString("Not found");
                    await writ.StoreAsync();
                }
            }
        }
    }
}
