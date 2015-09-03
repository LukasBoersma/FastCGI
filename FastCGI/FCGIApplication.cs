using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace FastCGI
{
    /// <summary>
    /// Main FastCGI listener class.
    /// </summary>
    /// 
    /// <remarks>
    /// This class manages a connection to a webserver by listening on a given port on localhost and receiving FastCGI
    /// requests by a webserver like Apache or nginx.
    ///
    /// In FastCGI terms, this class implements the responder role. Refer to section 6.2 of the FastCGI specification
    /// for details.
    /// 
    /// Use <see cref="OnRequestReceived"/> to get notified of received requests. You can call <see cref="Run"/> to
    /// enter an infinite loopand let the app handle everything.
    /// Alternatively, if you want to control the execution flow by yourself, call <see cref="Listen(int)"/> to start
    /// accepting connections. Then repeatedly call <see cref="Process"/> to handle incoming requests.
    /// 
    /// If you want to manage the socket connection details by yourself, or for testing purposes,
    /// you can also call <see cref="ProcessSingleRecord(Stream, Stream)"/> instead of any of the above methods.
    /// 
    /// See the below example to learn how to accept requests.
    /// For more detailed information, have a look at the <see cref="Request"/> class.
    /// 
    /// If you need to fiddle with the FastCGI packets itself, see the <see cref="Record"/> class and read the
    /// [FastCGI specification](http://www.fastcgi.com/devkit/doc/fcgi-spec.html).
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Create a new FCGIApplication, will accept FastCGI requests
    /// var app = new FCGIApplication();
    ///
    /// // Handle requests by responding with a 'Hello World' message
    /// app.OnRequestReceived += (sender, request) => {
    ///     request.WriteResponseASCII("Content-Type:text/html\n\nHello World!");
    ///     request.Close();
    /// };
    /// // Start listening on port 19000
    /// app.Run(19000);
    /// 
    /// // You now need a webserver like nginx or Apache to pass incoming requests
    /// // via FastCGI to your application.
    /// </code>
    /// </example>
    public class FCGIApplication
    {
        /// <summary>
        /// A dictionary of all open <see cref="Request">Requests</see>, indexed by the FastCGI request id.
        /// </summary>
        public Dictionary<int, Request> OpenRequests = new Dictionary<int, Request>();

        /// <summary>
        /// The network stream of the connection to the webserver.
        /// </summary>
        /// <remarks>
        /// Can be null if the application is currently not connected to a webserver.
        /// </remarks>
        public Stream CurrentStream = null;

        /// <summary>
        /// True iff this application is currently connected to a webserver.
        /// </summary>
        public bool Connected { get { return CurrentStream != null && !RequestFinished; } }

        /// <summary>
        /// True iff this application is about to close the connection to the webserver.
        /// </summary>
        public bool RequestFinished = false;

        /// <summary>
        /// Will be called whenever a request has been received.
        /// </summary>
        /// <remarks>
        /// Please note that multiple requests can be open at the same time.
        /// This means that this event may fire multiple times before you call <see cref="Request.Close"/> on the first one.
        /// </remarks>
        public event EventHandler<Request> OnRequestReceived = null;

        Socket ListeningSocket;
        Socket CurrentConnection;

        /// <summary>
        /// Indicates whether the current connection should be closed after the next request is closed.
        /// </summary>
        bool KeepConnection = false;

        /// <summary>
        /// Starts listening for connections on the given port.
        /// </summary>
        /// <remarks>
        /// Will only accept connections from localhost. Use the <see cref="Listen(IPEndPoint)"/> overload of this method to specify where to listen for connection.
        /// </remarks>
        public void Listen(int port)
        {
            Listen(new IPEndPoint(IPAddress.Loopback, port));
        }

        /// <summary>
        /// Starts listening for connections on the given IP end point.
        /// </summary>
        public void Listen(IPEndPoint endPoint)
        {
            if (ListeningSocket != null)
                throw new InvalidOperationException("Can not start listening while already listening.");

            ListeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ListeningSocket.Bind(endPoint);
            ListeningSocket.ReceiveTimeout = 5000;
            ListeningSocket.SendTimeout = 5000;
            ListeningSocket.Listen(1);
        }

        /// <summary>
        /// Processes all data available on the current FastCGI connection and handles the received data.
        /// </summary>
        /// <remarks>
        /// Call this repeatedly to process incoming requests.
        /// Alternatively, you can call <see cref="Run(int)"/> once, which will call <see cref="Listen(int)"/> and execute this method in an infinite loop.
        /// Internally, this method manages reconnections on the network socket and calls <see cref="ProcessSingleRecord(Stream, Stream)"/>.
        /// Returns true if a record was read, false otherwise.
        /// </remarks>
        public bool Process()
        {
            // When listening, but not currently connected, 
            if(ListeningSocket != null && (CurrentConnection == null || !CurrentConnection.Connected))
            {
                CurrentConnection = ListeningSocket.Accept();
                CurrentConnection.ReceiveTimeout = 5000;
                CurrentConnection.SendTimeout = 5000;
                var stream = new NetworkStream(CurrentConnection);
                stream.ReadTimeout = 5000;
                stream.WriteTimeout = 5000;

                CurrentStream = stream;

                KeepConnection = false;
            }

            if (CurrentStream != null)
            {
                return ProcessStream(CurrentStream, CurrentStream);
            }
            else return false;
        }

        /// <summary>
        /// Reads and handles all <see cref="Record">Records</see> available on the custom inputStream and writes responses to outputStream.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Process"/> if you don't need a custom stream, but instead want to process the records on the current FastCGI connection.
        /// Returns true if a record was read, false otherwise.
        /// </remarks>
        public bool ProcessStream(Stream inputStream, Stream outputStream)
        {
            bool readARecord = false;
            bool finished = false;
            while(!finished)
            {
                var gotRecord = ProcessSingleRecord(inputStream, outputStream);

                if(gotRecord)
                    readARecord = true;
                else
                    finished = true;
            }

            return readARecord;
        }

        /// <summary>
        /// Tries to read and handle a <see cref="Record"/> from inputStream and writes responses to outputStream.
        /// </summary>
        /// <remarks>
        /// Use <see cref="ProcessStream"/> to process all records on a stream.
        /// Returns true if a record was read, false otherwise.
        /// </remarks>
        public bool ProcessSingleRecord(Stream inputStream, Stream outputStream)
        {
            if (!inputStream.CanRead)
                return false;

            Record r = Record.ReadRecord(inputStream);

            // No record found on the stream?
            if (r == null)
            {
                return false;
            }

            if (r.Type == Record.RecordType.BeginRequest)
            {
                if (OpenRequests.ContainsKey(r.RequestId))
                    OpenRequests.Remove(r.RequestId);

                var content = new MemoryStream(r.ContentData);

                var role = Record.ReadInt16(content);
                // Todo: Refuse requests for other roles than FCGI_RESPONDER

                var flags = content.ReadByte();
                if ((flags & Constants.FCGI_KEEP_CONN) != 0)
                    KeepConnection = true;

                var request = new Request(r.RequestId, outputStream, this);
                OpenRequests.Add(request.RequestId, request);
            }
            else if (r.Type == Record.RecordType.AbortRequest || r.Type == Record.RecordType.EndRequest)
            {
                OpenRequests.Remove(r.RequestId);
            }
            else if (r.Type == Record.RecordType.GetValues)
            {
                var getValuesResult = Record.CreateGetValuesResult(1, 1, false);
                getValuesResult.Send(outputStream);
            }
            else
            {
                if (OpenRequests.ContainsKey(r.RequestId))
                {
                    var request = OpenRequests[r.RequestId];
                    bool requestReady = request.HandleRecord(r);
                    if (requestReady)
                    {
                        var evh = OnRequestReceived;
                        if (evh != null)
                            OnRequestReceived(this, request);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Used internally to notify the app that a <see cref="Request"/> has been closed.
        /// </summary>
        /// <param name="request"></param>
        internal void RequestClosed(Request request)
        {
            OpenRequests.Remove(request.RequestId);
            if(!KeepConnection)
            {
                CurrentConnection.Disconnect(true);
                CurrentStream.Close();
                CurrentStream = null;
            }
        }

        /// <summary>
        /// Stops listening for incoming connections.
        /// </summary>
        public void StopListening()
        {
            if (CurrentConnection != null && CurrentConnection.Connected)
            {
                CurrentConnection.Disconnect(true);
                CurrentConnection = null;
                CurrentStream = null;
            }

            if(ListeningSocket != null)
            {
                ListeningSocket.Close();
                ListeningSocket = null;
            }
        }

        /// <summary>
        /// This method never returns! Starts listening for FastCGI requests on the given port.
        /// </summary>
        /// <remarks>
        /// Use <see cref="OnRequestReceived"/> to react to incoming requests.
        /// Internally, this simply calls <see cref="Listen(int)"/> and enters an infinite loop of <see cref="Process()"/> calls.
        /// </remarks>
        public void Run(int port)
        {
            Listen(port);

            while (true)
            {
                var receivedARecord = Process();

                // If no records were processed, sleep for 1 millisecond until the next try to reduce CPU load
                if(!receivedARecord)
                    Thread.Sleep(1);
            }
        }

    }
}
