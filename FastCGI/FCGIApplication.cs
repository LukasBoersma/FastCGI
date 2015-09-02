using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;


namespace FastCGI
{
    /// <summary>
    /// Main FastCGI listener class.
    /// </summary>
    /// <remarks>
    /// This class manages a connection to a webserver by listening on a given port on localhost and receiving FastCGI requests by a webserver like Apache or nginx.
    /// Call <see cref="Run"/> to start listening, and use <see cref="OnRequestReceived"/> to get notified of received requests.
    /// 
    /// In FastCGI terms, this class implements the responder role. Refer to section 6.2 of the FastCGI specification for details.
    /// 
    /// See the below example to learn how to accept requests. For more complex usage. have a look at the <see cref="Request"/> class.
    /// If you need to go even deeper, see the <see cref="Record"/> class and read the [FastCGI specification](http://www.fastcgi.com/devkit/doc/fcgi-spec.html)
    /// </remarks>
    /// <example>
    /// <code>
    ///   // Create a new FCGIApplication, will accept FastCGI requests
    ///   var app = new FCGIApplication();
    ///
    ///   // Handle requests by responding with a 'Hello World' message
    ///   app.OnRequestReceived += (sender, request) => {
    ///       request.WriteBodyASCII("Content-Type:text/html\n\nHello World!");
    ///       request.Close();
    ///   };
    ///   // Start listening on port 19000
    ///   app.Run(19000);
    /// </code>
    /// </example>
    public class FCGIApplication
    {
        /// <summary>
        /// A dictionary of all open <see cref="Request">requests</see>, indexed by id.
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

        public void Listen(int port)
        {
            ListeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ListeningSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            ListeningSocket.ReceiveTimeout = 5000;
            ListeningSocket.SendTimeout = 5000;
            ListeningSocket.Listen(1);
        }

        public void Process()
        {
            if(CurrentConnection == null || !CurrentConnection.Connected)
            {
                CurrentConnection = ListeningSocket.Accept();
                CurrentConnection.ReceiveTimeout = 5000;
                CurrentConnection.SendTimeout = 5000;
                var stream = new NetworkStream(CurrentConnection);
                stream.ReadTimeout = 5000;
                stream.WriteTimeout = 5000;

                CurrentStream = stream;

                RequestFinished = false;
                KeepConnection = false;
            }

            Record r = Record.ReadRecord(CurrentStream);

            // Invalid record? Close connection.
            // Todo: Is this the correct behavior?
            if (r == null)
            {
                CurrentConnection.Disconnect(true);
                return;
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

                var request = new Request(r.RequestId, this);
                OpenRequests.Add(request.RequestId, request);
            }
            else if (r.Type == Record.RecordType.AbortRequest || r.Type == Record.RecordType.EndRequest)
            {
                OpenRequests.Remove(r.RequestId);
            }
            else if (r.Type == Record.RecordType.GetValues)
            {
                var getValuesResult = Record.CreateGetValuesResult(1, 1, false);
                SendRecord(getValuesResult);
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
            
            if (!KeepConnection && CurrentConnection.Connected && RequestFinished)
                CurrentConnection.Disconnect(true);
        }

        public void StopListening()
        {
            if (CurrentConnection != null && CurrentConnection.Connected)
            {
                CurrentConnection.Disconnect(true);
                CurrentConnection = null;
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
        public void Run(int port)
        {
            Listen(port);

            while (true)
            {
                Process();
            }
        }
        
        /// <summary>
        /// Used internally. Sends the given record to the webserver.
        /// </summary>
        internal void SendRecord(Record r)
        {
            var memStr = new MemoryStream();
            memStr.Capacity = r.ContentLength + Constants.FCGI_HEADER_LEN;

            int recordSize = r.WriteToStream(memStr);
            CurrentStream.Write(memStr.GetBuffer(), 0, recordSize);
        }
    }
}
