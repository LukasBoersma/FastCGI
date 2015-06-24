using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace FastCGI
{
    /// <summary>
    /// Represents a FastCGI Record.
    /// Described in the FastCGI Specification section 3.3.
    /// Contains only the relevant part of a record, reserved bytes and padding are not included.
    /// </summary>
    public class Record
    {
        /// <summary>
        /// Record types, used in the 'type' field of Record.
        /// Described in the FastCGI Specification section 8.
        /// </summary>
        public enum RecordType : byte
        {
            BeginRequest = Constants.FCGI_BEGIN_REQUEST,
            AbortRequest,
            EndRequest,
            Params,
            Stdin,
            Stdout,
            Stderr,
            Data,
            GetValues,
            GetValuesResult,
            UnknownType = Constants.FCGI_UNKNOWN_TYPE,
            MaxType = Constants.FCGI_MAXTYPE
        }

        /// <summary>
        /// Protocol status used for requests.
        /// Described in the FastCGI Specification section 8.
        /// </summary>
        public enum ProtocolStatus : byte
        {
            RequestComplete = Constants.FCGI_REQUEST_COMPLETE,
            CantMpxConn = Constants.FCGI_CANT_MPX_CONN,
            Overloaded = Constants.FCGI_OVERLOADED,
            UnknownRole = Constants.FCGI_UNKNOWN_ROLE
        }

        public byte version = Constants.FCGI_VERSION_1;
        public RecordType type;
        public int requestId;
        public int contentLength = 0;
        public byte[] contentData;

        /// <summary>
        /// Tries to read a dictionary of name-value pairs from the record content.
        /// Will return nonsense or throw an EndOfStreamException if the record content does not contain valid name-value pairs.
        /// </summary>
        public Dictionary<string, byte[]> GetNameValuePairs()
        {
            var nameValuePairs = new Dictionary<string, byte[]>();
            var stream = new MemoryStream(contentData);

            while(stream.Position < stream.Length)
            {
                int nameLength = ReadVarLength(stream);
                int valueLength = ReadVarLength(stream);

                byte[] name = new byte[nameLength];

                stream.Read(name, 0, nameLength);

                byte[] value = new byte[valueLength];

                stream.Read(value, 0, valueLength);

                nameValuePairs.Add(Encoding.ASCII.GetString(name), value);
            }

            return nameValuePairs;
        }

        /// <summary>
        /// Sets the record content to a given dictionary of name-value pairs
        /// </summary>
        public void SetNameValuePairs(Dictionary<string, byte[]> nameValuePairs)
        {
            MemoryStream stream = new MemoryStream();
            stream.Capacity = 4096;

            // Write names
            foreach (var nameValuePair in nameValuePairs)
            {
                string name = nameValuePair.Key;
                byte[] nameBuf = Encoding.ASCII.GetBytes(name);
                byte[] value = nameValuePair.Value;

                WriteVarLength(stream, nameBuf.Length);
                WriteVarLength(stream, value.Length);
                
                stream.Write(nameBuf, 0, nameBuf.Length);
                stream.Write(value, 0, value.Length);
            }

            contentLength = (int)stream.Length;
            contentData = stream.GetBuffer();
        }

        /// <summary>
        /// Reads a length from the stream, which is encoded in one or four bytes, as described in section 3.4 of the FastCGI specification.
        /// </summary>
        static int ReadVarLength(Stream stream)
        {
            byte firstByte = ReadByte(stream);
            // length values < 127 are encoded in a single byte
            if (firstByte <= 127)
            {
                return firstByte;
            }
            else
            {
                byte b2 = ReadByte(stream);
                byte b1 = ReadByte(stream);
                byte b0 = ReadByte(stream);
                return 16777216 * firstByte + 65536 * b2 + 256 * b1 + b0;
            }
        }

        static byte ReadByte(Stream stream)
        {
            int result = stream.ReadByte();
            if (result < 0)
                throw new EndOfStreamException();
            return (byte)result;
        }

        static Int16 ReadInt16(Stream stream)
        {
            byte h = ReadByte(stream);
            byte l = ReadByte(stream);
            return (short)(h * 256 + l);
        }

        static void WriteInt16(Stream stream, Int16 v)
        {
            stream.WriteByte((byte)(v/256));
            stream.WriteByte((byte)(v));
        }

        static void WriteVarLength(Stream stream, int len)
        {
            if (len <= 127)
                stream.WriteByte((byte)len);
            else
            {
                stream.WriteByte((byte)(len / 16777216));
                stream.WriteByte((byte)(len / 65536));
                stream.WriteByte((byte)(len / 256));
                stream.WriteByte((byte)(len));
            }
        }

        /// <summary>
        /// Reads a single Record from the given stream.
        /// </summary>
        /// <returns>Returns the retreived record or null if no record could be read.</returns>
        public static Record ReadRecord(Stream stream)
        {
            Record r = new Record();

            try
            {

                r.version = ReadByte(stream);
                r.type = (Record.RecordType)ReadByte(stream);
                r.requestId = ReadInt16(stream);
                r.contentLength = ReadInt16(stream); ;
                byte paddingLength = ReadByte(stream);

                // Skip reserved byte
                ReadByte(stream);

                r.contentData = new byte[r.contentLength];

                // Read content
                if(r.contentLength > 0)
                    stream.Read(r.contentData, 0, r.contentLength);

                // Skip padding data
                if (paddingLength > 0)
                {
                    byte[] ignoredBuf = new byte[paddingLength];
                    stream.Read(ignoredBuf, 0, paddingLength);
                }

            }
            catch (EndOfStreamException e)
            {
                // Connection has been closed while reading a Record. Return a null record.
                return null;
            }

            return r;
        }
        
        /// <summary>
        /// Writes this record to the given stream.
        /// </summary>
        /// <returns>Returns the number of bytes written.</returns>
        public int WriteToStream(Stream stream)
        {
            stream.WriteByte(version);
            stream.WriteByte((byte)type);
            WriteInt16(stream, (Int16)requestId);
            WriteInt16(stream, (Int16)contentLength);

            // No padding
            stream.WriteByte(0);
            // Reserved byte
            stream.WriteByte(0);

            if(contentLength > 0)
            stream.Write(contentData, 0, contentLength);

            return Constants.FCGI_HEADER_LEN + contentLength;
        }

        /// <summary>
        /// Creates a Stdout record from the given data and request id
        /// </summary>
        public static Record CreateStdout(byte[] data, int requestId)
        {
            return new Record
            {
                type = Record.RecordType.Stdout,
                requestId = requestId,
                contentLength = data.Length,
                contentData = data
            };
        }

        /// <summary>
        /// Creates a EndRequest record with the given request id
        /// </summary>
        public static Record CreateEndRequest(int requestId)
        {
            byte[] content = new byte[8];

            // appStatusB3 - appStatusB0
            content[0] = (byte)0;
            content[1] = (byte)0;
            content[2] = (byte)0;
            content[3] = (byte)0;

            // protocolStatus
            content[4] = (byte)ProtocolStatus.RequestComplete;

            // reserved bytes
            content[5] = 0;
            content[6] = 0;
            content[7] = 0;

            return new Record
            {
                type = Record.RecordType.EndRequest,
                requestId = requestId,
                contentLength = content.Length,
                contentData = content
            };
        }

        /// <summary>
        /// Creates a GetValuesResult record from the given config values.
        /// </summary>
        public static Record CreateGetValuesResult(int maxConnections, int maxRequests, bool multiplexing)
        {
            var nameValuePairs = new Dictionary<string, byte[]>();

            nameValuePairs.Add(Constants.FCGI_MAX_CONNS, Encoding.ASCII.GetBytes(maxConnections.ToString()));
            nameValuePairs.Add(Constants.FCGI_MAX_REQS, Encoding.ASCII.GetBytes(maxRequests.ToString()));
            nameValuePairs.Add(Constants.FCGI_MPXS_CONNS, Encoding.ASCII.GetBytes(multiplexing ? "1" : "0"));

            var record = new Record
            {
                requestId = 0,
                type = RecordType.GetValuesResult
            };

            record.SetNameValuePairs(nameValuePairs);

            return record;
        }

        public override string ToString()
        {
            return "{Record type: " + type.ToString() + ", requestId: " + requestId.ToString() + "}";
        }
    }
}
