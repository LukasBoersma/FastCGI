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
    public class FCGIApplication
    {
        public Dictionary<int, Request> OpenRequests = new Dictionary<int, Request>();

        public Stream CurrentStream = null;

        public bool Disconnecting = false;

        /// <summary>
        /// Will be called whenever a request has been received.
        /// </summary>
        public EventHandler<Request> OnRequestReceived = null;

        /// <summary>
        /// Start listening for FastCGI requests on the given port
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

                    if(r.type == Record.RecordType.BeginRequest)
                    {
                        if (OpenRequests.ContainsKey(r.requestId))
                            OpenRequests.Remove(r.requestId);

                        var request = new Request(r.requestId, this);
                        OpenRequests.Add(request.RequestId, request);
                    }
                    else if (r.type == Record.RecordType.AbortRequest || r.type == Record.RecordType.EndRequest)
                    {
                        OpenRequests.Remove(r.requestId);
                    }
                    else
                    {
                        var request = OpenRequests[r.requestId];
                        bool requestReady = request.HandleRecord(r);
                        if(requestReady)
                        {
                            var evh = OnRequestReceived;
                            if (evh != null)
                                OnRequestReceived(this, request);
                        }
                    }
                }

                conn.Disconnect(true);
            }
        }
        
        public void SendRecord(Record r)
        {
            var memStr = new MemoryStream();
            memStr.Capacity = 4096;

            int recordSize = r.WriteToStream(memStr);
            CurrentStream.Write(memStr.GetBuffer(), 0, recordSize);
        }
    }
}
