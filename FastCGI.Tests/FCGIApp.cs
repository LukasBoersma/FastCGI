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

        [Test]
        public void FCGIApp_Request()
        {
            var app = new FCGIApplication();

            app.OnRequestReceived += (sender, request) =>
            {
                request.WriteResponseASCII("Hello!");
                request.Close();
            };

            // Connect to the app, impoersonating a webserver
            var streamServerToApp = new MemoryStream();
            streamServerToApp.Capacity = 4096;
            var streamAppToServer = new MemoryStream();
            streamAppToServer.Capacity = 4096;

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

            beginRequest.WriteToStream(streamServerToApp);

            // Empty stdin indicates that the request is fully transmitted
            var stdinRequest = new Record
            {
                Type = Record.RecordType.Stdin,
                RequestId = requestId,
                ContentLength = 0,
                ContentData = new byte[0]
            };

            stdinRequest.WriteToStream(streamServerToApp);

            streamServerToApp.Seek(0, SeekOrigin.Begin);

            app.ProcessStream(streamServerToApp, streamAppToServer);;

            streamAppToServer.Seek(0, SeekOrigin.Begin);

            // Now the app should respond with 'Hello!'

            var response = Record.ReadRecord(streamAppToServer);

            var expectedBytes = Encoding.ASCII.GetBytes("Hello!");

            Assert.AreEqual(Record.RecordType.Stdout, response.Type);
            Assert.AreEqual(requestId, response.RequestId);
            Assert.AreEqual(expectedBytes.Length, response.ContentLength);
            Assert.AreEqual(expectedBytes, response.ContentData);

            // Then, an empty stdout record should indicate the end of the response body
            response = Record.ReadRecord(streamAppToServer);
            Assert.AreEqual(Record.RecordType.Stdout, response.Type);
            Assert.AreEqual(requestId, response.RequestId);
            Assert.AreEqual(0, response.ContentLength);

            // And finally, a EndRequest record should close the request.
            response = Record.ReadRecord(streamAppToServer);
            Assert.AreEqual(Record.RecordType.EndRequest, response.Type);
            Assert.AreEqual(requestId, response.RequestId);
        }

        [Test]
        public void FCGIApp_LargeRequest()
        {
            var app = new FCGIApplication();

            var expectedLength = 128317;

            app.OnRequestReceived += (sender, request) =>
            {
                var responseBody = new byte[expectedLength];
                request.WriteResponse(responseBody);
                request.Close();
            };

            // Connect to the app, impoersonating a webserver
            var inputStream = new MemoryStream();
            inputStream.Capacity = 4096;
            var outputStream = new MemoryStream();
            outputStream.Capacity = 4096 + expectedLength;

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

            beginRequest.WriteToStream(inputStream);

            // Empty stdin indicates that the request is fully transmitted
            var stdinRequest = new Record
            {
                Type = Record.RecordType.Stdin,
                RequestId = requestId,
                ContentLength = 0,
                ContentData = new byte[0]
            };

            stdinRequest.WriteToStream(inputStream);

            inputStream.Seek(0, SeekOrigin.Begin);

            app.ProcessStream(inputStream, outputStream);

            outputStream.Seek(0, SeekOrigin.Begin);

            // Now the app should respond with a large response, splitted into 64KB stdout records

            var response = Record.ReadRecord(outputStream);

            Assert.AreEqual(65535, response.ContentLength);

            var totalLength = 0;

            while (response.ContentLength > 0)
            {
                Assert.AreEqual(Record.RecordType.Stdout, response.Type);
                Assert.AreEqual(requestId, response.RequestId);

                totalLength += response.ContentLength;

                response = Record.ReadRecord(outputStream);
            }

            Assert.AreEqual(expectedLength, totalLength);

            // Then, an empty stdout record should indicate the end of the response body
            Assert.AreEqual(Record.RecordType.Stdout, response.Type);
            Assert.AreEqual(requestId, response.RequestId);
            Assert.AreEqual(0, response.ContentLength);

            // And finally, a EndRequest record should close the request.
            response = Record.ReadRecord(outputStream);
            Assert.AreEqual(Record.RecordType.EndRequest, response.Type);
            Assert.AreEqual(requestId, response.RequestId);
        }

        [Test]
        public void FCGIApp_Params()
        {
            var app = new FCGIApplication();

            Request receivedRequest = null;

            app.OnRequestReceived += (sender, request) =>
            {
                receivedRequest = request;
            };

            // Connect to the app, impoersonating a webserver
            var streamServerToApp = new MemoryStream();
            streamServerToApp.Capacity = 4096;
            var streamAppToServer = new MemoryStream();
            streamAppToServer.Capacity = 4096;

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

            beginRequest.WriteToStream(streamServerToApp);

            var paramDict = new Dictionary<string, byte[]>
            {
                {"SOME_NAME", Encoding.UTF8.GetBytes("SOME_KEY") },
                {"SOME_OTHER_NAME", Encoding.UTF8.GetBytes("SOME_KEY") },
                //{"ONE_MEGABYTE_OF_ZEROS", new byte[1024 * 1024] },
                {"EMPTY_VALUE", new byte[0] },
                {"1", Encoding.UTF8.GetBytes("☕☳üß - \n \r\n .,;(){}%$!") },
                {"2", Encoding.UTF8.GetBytes("") },
                {"3", Encoding.UTF8.GetBytes(" ") },
                {"4", Encoding.UTF8.GetBytes("\n") },
                {"5", Encoding.UTF8.GetBytes(";") },
            };

            var paramsRequest = new Record
            {
                Type = Record.RecordType.Params,
                RequestId = requestId
            };

            paramsRequest.SetNameValuePairs(paramDict);
            paramsRequest.WriteToStream(streamServerToApp);

            // Empty params record indicates that the parameters are fully transmitted
            var paramsCloseRequest = new Record
            {
                Type = Record.RecordType.Params,
                RequestId = requestId,
                ContentLength = 0,
                ContentData = new byte[0]
            };
            paramsCloseRequest.WriteToStream(streamServerToApp);


            // Empty stdin indicates that the request is fully transmitted
            var stdinRequest = new Record
            {
                Type = Record.RecordType.Stdin,
                RequestId = requestId,
                ContentLength = 0,
                ContentData = new byte[0]
            };

            stdinRequest.WriteToStream(streamServerToApp);

            streamServerToApp.Seek(0, SeekOrigin.Begin);

            app.ProcessStream(streamServerToApp, streamAppToServer);

            // Now the app should have received the request. Make sure the Parameters correctly decoded
            Assert.IsNotNull(receivedRequest);
            Assert.AreEqual(paramDict.Count, receivedRequest.Parameters.Count);

            foreach (var entry in paramDict)
            {
                Assert.Contains(entry.Key, receivedRequest.Parameters.Keys);
                Assert.AreEqual(entry.Value, receivedRequest.Parameters[entry.Key]);
            }
        }

        [Test]
        public void FCGIApp_GetValues()
        {
            var app = new FCGIApplication();

            // Connect to the app, impoersonating a webserver
            var inputStream = new MemoryStream();
            inputStream.Capacity = 4096;
            var outputStream = new MemoryStream();
            outputStream.Capacity = 4096;

            // Send a GetValues record
            var getValues = new Record
            {
                Type = Record.RecordType.GetValues,
                RequestId = 0,
                ContentLength = 0,
            };

            getValues.WriteToStream(inputStream);
            inputStream.Seek(0, SeekOrigin.Begin);
            app.ProcessStream(inputStream, outputStream);
            outputStream.Seek(0, SeekOrigin.Begin);

            // Now the app should respond with a GetValuesResult
            var response = Record.ReadRecord(outputStream);

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

    }
}
