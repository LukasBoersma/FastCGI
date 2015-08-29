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
    /// If you need to go even deeper, see the <see cref="Record"/> class and read the FastCGI specification: http://www.fastcgi.com/devkit/doc/fcgi-spec.html
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
        public bool Connected { get { return CurrentStream != null && !Disconnecting; } }

        /// <summary>
        /// True iff this application is about to close the connection to the webserver.
        /// </summary>
        public bool Disconnecting = false;

        /// <summary>
        /// Will be called whenever a request has been received.
        /// </summary>
        /// <remarks>
        /// Please note that multiple requests can be open at the same time.
        /// This means that this event may fire multiple times before you call <see cref="Request.Close"/> on the first one.
        /// </remarks>
        public event EventHandler<Request> OnRequestReceived = null;

        /// <summary>
        /// This method never returns! Starts listening for FastCGI requests on the given port.
        /// </summary>
        public void Run(int port)
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(new IPEndPoint(IPAddress.Loopback, port));
            sock.Listen(1);

            while (true)
            {
                var conn = sock.Accept();
                var stream = new NetworkStream(conn);
                CurrentStream = stream;

                Disconnecting = false;

                while (conn.Connected && !Disconnecting)
                {
                    Record r = Record.ReadRecord(stream);

                    // Invalid record? Close connection.
                    // Todo: Is this the correct behavior?
                    if (r == null)
                    {
                        conn.Disconnect(true);
                        continue;
                    }

                    if(r.Type == Record.RecordType.BeginRequest)
                    {
                        if (OpenRequests.ContainsKey(r.RequestId))
                            OpenRequests.Remove(r.RequestId);

                        var request = new Request(r.RequestId, this);
                        OpenRequests.Add(request.RequestId, request);
                    }
                    else if (r.Type == Record.RecordType.AbortRequest || r.Type == Record.RecordType.EndRequest)
                    {
                        OpenRequests.Remove(r.RequestId);
                    }
                    else
                    {
                        var request = OpenRequests[r.RequestId];
                        bool requestReady = request.HandleRecord(r);
                        if(requestReady)
                        {
                            var evh = OnRequestReceived;
                            if (evh != null)
                                OnRequestReceived(this, request);
                        }
                    }
                }

                if(conn.Connected)
                    conn.Disconnect(true);
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
