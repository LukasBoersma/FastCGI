using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

using NUnit.Framework;

using FastCGI;

namespace FastCGI.Tests
{
    [TestFixture]
    class FCGIApp
    {
        const int TESTING_PORT = 31289;

        bool AppRunning;

        private void FCGIRunner()
        {
            var app = new FCGIApplication();

            app.OnRequestReceived += (sender, request) => {
                request.WriteResponseASCII("Hello!");
                request.Close();
            };

            app.Listen(TESTING_PORT);

            while(AppRunning)
            {
                app.Process();
            }

            app.StopListening();
        }

        private Stream ConnectToFcgiTestApp()
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.ReceiveTimeout = 200;
            sock.SendTimeout = 200;
            sock.Connect(new IPEndPoint(IPAddress.Loopback, TESTING_PORT));

            var stream = new NetworkStream(sock);
            stream.ReadTimeout = 200;
            stream.WriteTimeout = 200;

            return stream;
        }

        [Test]
        public void FCGIApp_Communication()
        {
            // Start a FCGIApplication in a new thread that actually listens on an internal port.
            AppRunning = true;
            var thread = new Thread(FCGIRunner);
            try
            {
                thread.Start();

                // Connect to the app, impoersonating a webserver
                var stream = ConnectToFcgiTestApp();

                // Send a request to it and make sure it responds to requests.

                var requestId = 5172;

                var beginRequestContent = new byte[]
                {
                    0x00, // role byte 1
                    (byte)Constants.FCGI_RESPONDER, // role byte 2
                    Constants.FCGI_KEEP_CONN, // flags
                    0x00, 0x00, 0x00, 0x00, 0x00 // reserved bytes
                };

                var beginRequest = new Record
                {
                    Type = Record.RecordType.BeginRequest,
                    RequestId = requestId,
                    ContentLength = beginRequestContent.Length,
                    ContentData = beginRequestContent
                };

                beginRequest.WriteToStream(stream);

                var stdinRequest = new Record
                {
                    Type = Record.RecordType.Stdin,
                    RequestId = requestId,
                    ContentLength = 0,
                    ContentData = new byte[0]
                };

                stdinRequest.WriteToStream(stream);

                // Now the app should respond with 'Hello!'

                var response = Record.ReadRecord(stream);

                var expectedBytes = Encoding.ASCII.GetBytes("Hello!");

                Assert.AreEqual(Record.RecordType.Stdout, response.Type);
                Assert.AreEqual(requestId, response.RequestId);
                Assert.AreEqual(expectedBytes.Length, response.ContentLength);
                Assert.AreEqual(expectedBytes, response.ContentData);

                // Then, an empty stdout record should indicate the end of the response body
                response = Record.ReadRecord(stream);
                Assert.AreEqual(Record.RecordType.Stdout, response.Type);
                Assert.AreEqual(requestId, response.RequestId);
                Assert.AreEqual(0, response.ContentLength);

                // And finally, a EndRequest record should close the request.
                response = Record.ReadRecord(stream);
                Assert.AreEqual(Record.RecordType.EndRequest, response.Type);
                Assert.AreEqual(requestId, response.RequestId);

                // Send a GetValues record
                var getValues = new Record
                {
                    Type = Record.RecordType.GetValues,
                    RequestId = 0,
                    ContentLength = 0,
                };

                getValues.WriteToStream(stream);

                // Now the app should respond with a GetValuesResult
                response = Record.ReadRecord(stream);

                Assert.AreEqual(Record.RecordType.GetValuesResult, response.Type);
                Assert.AreEqual(0, response.RequestId);
                
                var responseValues = response.GetNameValuePairs();

                // Response should include these three values
                Assert.Contains("FCGI_MAX_CONNS", responseValues.Keys);
                Assert.Contains("FCGI_MAX_REQS", responseValues.Keys);
                Assert.Contains("FCGI_MPXS_CONNS", responseValues.Keys);

                // No other values should be included
                Assert.AreEqual(3, responseValues.Count);

                int responseMaxConns, responseMaxReqs, responseMultiplexing;

                // Make sure the returned values look plausible
                var sMaxConns = Encoding.ASCII.GetString(responseValues["FCGI_MAX_CONNS"]);
                var sMaxReqs = Encoding.ASCII.GetString(responseValues["FCGI_MAX_REQS"]);
                var sMultiplexing = Encoding.ASCII.GetString(responseValues["FCGI_MPXS_CONNS"]);

                Assert.IsTrue(int.TryParse(sMaxConns, out responseMaxConns));
                Assert.IsTrue(int.TryParse(sMaxReqs, out responseMaxReqs));
                Assert.IsTrue(int.TryParse(sMultiplexing, out responseMultiplexing));

                Assert.GreaterOrEqual(responseMaxConns, 0);
                Assert.GreaterOrEqual(responseMaxReqs, 0);
                Assert.GreaterOrEqual(responseMultiplexing, 0);
                Assert.LessOrEqual(responseMultiplexing, 1);
            }
            finally
            {
                AppRunning = false;
                Thread.Sleep(200);
                if(thread.IsAlive)
                    thread.Abort();
            }
        }

    }
}
