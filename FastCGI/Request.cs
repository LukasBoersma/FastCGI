using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastCGI
{
    /// <summary>
    /// A FastCGI request.
    /// </summary>
    /// <remarks>
    /// A request usually corresponds to a HTTP request that has been received by the webserver.
    /// You will probably want to use <see cref="WriteResponse"/> or its helper methods to output a response and then call <see cref="Close"/>.
    /// Use <see cref="FCGIApplication.OnRequestReceived"/> to be notified of new requests.
    /// Refer to the FastCGI specification for more details.
    /// </remarks>
    public class Request
    {
        public Request(int requestId, FCGIApplication app)
        {
            this.RequestId = requestId;
            this.app = app;
            Body = "";
        }

        FCGIApplication app;

        /// <summary>
        /// The id for this request, issued by the webserver
        /// </summary>
        public int RequestId { get; private set; }
        
        /// <summary>
        /// The FastCGI parameters passed by the webserver.
        /// </summary>
        /// <remarks>
        /// All strings are encoded in ASCII, regardless of any encoding information in the request.
        /// </remarks>
        public Dictionary<string, string> Parameters = new Dictionary<string,string>();

        /// <summary>
        /// The request body.
        /// </summary>
        /// <remarks>
        /// For POST requests, this will contain the POST variables. For GET requests, this will be empty.
        /// </remarks>
        public string Body { get; private set; }

        /// <summary>
        /// Used internally. Feeds a <see cref="Record">Record</see> to this request for processing.
        /// </summary>
        /// <param name="record">The record to feed.</param>
        /// <returns>Returns true iff the request is completely received.</returns>
        internal bool HandleRecord(Record record)
        {
            switch(record.Type)
            {
                case Record.RecordType.Params:
                    var parameters = record.GetNameValuePairs();
                    foreach(var param in parameters)
                    {
                        Parameters.Add(param.Key, Encoding.ASCII.GetString(param.Value));
                    }
                    break;
                case Record.RecordType.Stdin:
                    string data = Encoding.ASCII.GetString(record.ContentData);
                    Body += data;

                    // Finished requests are indicated by an empty stdin record
                    if (record.ContentLength == 0)
                        return true;

                    break;
            }

            return false;
        }

        /// <summary>
        /// Appends data to the response body.
        /// </summary>
        /// <remarks>
        /// The given data will be sent immediately to the webserver as a single stdout record.
        /// </remarks>
        /// <param name="data">The data to append.</param>
        public void WriteResponse(byte[] data)
        {
            // Todo: Handle data larger than 64KB
            var record = Record.CreateStdout(data, RequestId);
            app.SendRecord(record);
        }

        /// <summary>
        /// Appends an ASCII string to the response body.
        /// </summary>
        /// <remarks>
        /// This is a helper function, it converts the given string to ASCII bytes and feeds it to <see cref="WriteResponse"/>.
        /// </remarks>
        /// <param name="data">The string to append, encoded in ASCII.</param>
        /// <seealso cref="WriteResponse"/>
        /// <seealso cref="WriteResponseUtf8"/>
        public void WriteResponseASCII(string data)
        {
            var bytes = Encoding.ASCII.GetBytes(data);
            WriteResponse(bytes);
        }

        /// <summary>
        /// Appends an UTF-8 string to the response body.
        /// </summary>
        /// <remarks>
        /// This is a helper function, it converts the given string to UTF-8 bytes and feeds it to <see cref="WriteResponse"/>.
        /// </remarks>
        /// <param name="data">The string to append, encoded in UTF-8.</param>
        /// <seealso cref="WriteResponse"/>
        /// <seealso cref="WriteResponseASCII"/>
        public void WriteResponseUtf8(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            WriteResponse(bytes);
        }

        /// <summary>
        /// Closes this request.
        /// </summary>
        public void Close()
        {
            WriteResponse(Encoding.ASCII.GetBytes(""));
            var record = Record.CreateEndRequest(RequestId);
            app.SendRecord(record);
            app.Disconnecting = true;
        }
    }
}
