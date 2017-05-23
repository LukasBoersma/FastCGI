using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace FastCGI
{
    /// <summary>
    /// Represents a FastCGI connection to a webserver.
    /// </summary>
    /// <remarks>
    /// At any given time, a single <see cref="FCGIApplication"/> may have any number of open FCGIStream connections (including zero).
    /// No attempt is made to limit that number.
    /// This is basically just an extension of <see cref="NetworkStream"/>, with the underlying <see cref="Socket"/> exposed.
    /// </remarks>
    public class FCGIStream: NetworkStream
    {
        /// <summary>
        /// Creates a new FastCGI connection from a given socket connection.
        /// </summary>
        public FCGIStream(Socket socket) :
            base(socket)
        { }

        /// <summary>
        /// True iff the connection is still open.
        /// </summary>
        public bool IsConnected {  get { return Socket != null && Socket.Connected; } }

        /// <summary>
        /// The underlying socket of the 
        /// </summary>
        public new Socket Socket { get { return base.Socket; } }

        public void Disconnect()
        {
            if (Socket != null)
            {
                if (IsConnected)
                    Socket.Disconnect(false);
                Socket.Close();
            }
        }
    }
}
