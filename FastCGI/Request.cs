using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastCGI
{
    /// <summary>
    /// 
    /// </summary>
    public class Request
    {
        public Request(int requestId, FCGIApplication app)
        {
            this.RequestId = requestId;
            this.app = app;
        }

        FCGIApplication app;

        public int RequestId;
        
        public Dictionary<string, string> Parameters;

        public bool HandleRecord(Record record)
        {
            switch(record.type)
            {
                case Record.RecordType.Params:
                    // Todo: Save parameters
                    break;
                case Record.RecordType.Stdin:
                    return true;
                    break;
            }

            return false;
        }

        public void WriteBodyASCII(string data)
        {
            var bytes = Encoding.ASCII.GetBytes(data);
            WriteBody(bytes);
        }

        public void WriteBody(byte[] data)
        {
            // Todo: Handle data larger than 64KB
            var record = Record.CreateStdout(data, RequestId);
            app.SendRecord(record);
        }

        public void Close()
        {
            WriteBody(Encoding.ASCII.GetBytes(""));
            var record = Record.CreateEndRequest(RequestId);
            app.SendRecord(record);
            app.Disconnecting = true;
        }
    }
}
