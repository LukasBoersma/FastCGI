using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastCGI
{
    /// <summary>
    /// A FastCGI request.
    /// </summary>
    /// <remarks>
    /// A request usually corresponds to a HTTP request that has been received by the webserver (see the [FastCGI specification](http://www.fastcgi.com/devkit/doc/fcgi-spec.html) for details).
    /// 
    /// You will probably want to use <see cref="WriteResponse"/> or its helper methods to output a response and then call <see cref="Close"/>. Use <see cref="FCGIApplication.OnRequestReceived"/> to be notified of new requests.
    /// 
    /// Remember to call <see cref="Close"/> when you wrote the complete response. 
    /// </remarks>
    public class Request
    {
        /// <summary>
        /// Creates a new request. Usually, you don't need to call this.
        /// </summary>
        /// <remarks> Records are created by <see cref="FCGIApplication"/> when a new request has been received.</remarks>
        public Request(int requestId, Stream responseStream, FCGIApplication app = null)
        {
            this.RequestId = requestId;
            Body = "";
            ResponseStream = responseStream;
            ParamStream = new MemoryStream();
            ManagingApp = app;
        }

        /// <summary>
        /// The stream where responses to this request should be written to.
        /// Only write FastCGI records here, not the raw response body. Use <see cref="WriteResponse"/> for sending response data.
        /// </summary>
        Stream ResponseStream;

        /// <summary>
        /// The FCGIApplication that manages this requests. Can be null if this request is not associated with any FCGIApplication.
        /// </summary>
        /// <remarks>The request will notify this app about certain events, for example when the request is closed.</remarks>
        FCGIApplication ManagingApp;

        /// <summary>
        /// The id for this request, issued by the webserver
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// The FastCGI parameters received by the webserver, in raw byte arrays.
        /// </summary>
        /// <remarks>
        /// Use <see cref="GetParameterASCII(string)"/> and <see cref="GetParameterUTF8(string)"/> to get strings instead of byte arrays.
        /// </remarks>
        public Dictionary<string, byte[]> Parameters = new Dictionary<string, byte[]>();

        /// <summary>
        /// Returns the parameter with the given name as an ASCII encoded string.
        /// </summary>
        public string GetParameterASCII(string name)
        {
            return Encoding.ASCII.GetString(Parameters[name]);
        }

        /// <summary>
        /// Returns the parameter with the given name as an UTF-8 encoded string.
        /// </summary>
        public string GetParameterUTF8(string name)
        {
            return Encoding.UTF8.GetString(Parameters[name]);
        }

        /// <summary>
        /// The request body.
        /// </summary>
        /// <remarks>
        /// For POST requests, this will contain the POST variables. For GET requests, this will be empty.
        /// </remarks>
        public string Body { get; private set; }

        /// <summary>
        /// Incoming parameter records are stored here, until the parameter stream is closed by the webserver by sending an empty param record.
        /// </summary>
        MemoryStream ParamStream;

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

                    if (record.ContentLength == 0)
                    {
                        ParamStream.Seek(0, SeekOrigin.Begin);
                        Parameters = Record.ReadNameValuePairs(ParamStream);
                    }
                    else
                    {
                        // If the params are not yet finished, write the contents to the ParamStream.
                        ParamStream.Write(record.ContentData, 0, record.ContentLength);
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
            int remainingLength = data.Length;

            // Send data with at most 65535 bytes in one record
            if(remainingLength <= 65535)
            {
                var record = Record.CreateStdout(data, RequestId);
                record.Send(ResponseStream);
            }
            // Split data with more than 64KB into multiple records
            else
            {
                var buf64kb = new byte[65535];
                int offset = 0;
                while (remainingLength > 65535)
                {
                    Buffer.BlockCopy(data, offset, buf64kb, 0, 65535);

                    var record = Record.CreateStdout(buf64kb, RequestId);
                    record.Send(ResponseStream);

                    offset += 65535;
                    remainingLength -= 65535;
                }

                // Write the remaining data
                byte[] remainingBuf = new byte[remainingLength];
                Buffer.BlockCopy(data, offset, remainingBuf, 0, remainingLength);

                var remainingRecord = Record.CreateStdout(remainingBuf, RequestId);
                remainingRecord.Send(ResponseStream);
            }

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
            WriteResponse(new byte[0]);
            var record = Record.CreateEndRequest(RequestId);
            record.Send(ResponseStream);

            if (ManagingApp != null)
                ManagingApp.RequestClosed(this);
        }

    }
}
