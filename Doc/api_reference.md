<a id="FastCGI.FCGIApplication"></a>
## class FastCGI.FCGIApplication

Main FastCGI listener class.

This class manages a connection to a webserver by listening on a given port on localhost and receiving FastCGI
requests by a webserver like Apache or nginx.

In FastCGI terms, this class implements the responder role. Refer to section 6.2 of the FastCGI specification
for details.

Use [OnRequestReceived](#FastCGI.FCGIApplication.OnRequestReceived) to get notified of received requests. You can call [Run](#FastCGI.FCGIApplication.Run(System.Int32)) to
enter an infinite loopand let the app handle everything.
Alternatively, if you want to control the execution flow by yourself, call [Listen](#FastCGI.FCGIApplication.Listen(System.Int32)) to start
accepting connections. Then repeatedly call [Process](#FastCGI.FCGIApplication.Process) to handle incoming requests.

If you want to manage the socket connection details by yourself, or for testing purposes,
you can also call [ProcessSingleRecord](#FastCGI.FCGIApplication.ProcessSingleRecord(System.IO.Stream,System.IO.Stream)) instead of any of the above methods.

See the below example to learn how to accept requests.
For more detailed information, have a look at the [Request](#FastCGI.Request) class.

If you need to fiddle with the FastCGI packets itself, see the [Record](#FastCGI.Record) class and read the
[FastCGI specification](http://www.fastcgi.com/devkit/doc/fcgi-spec.html).

**Examples**


```csharp
// Create a new FCGIApplication, will accept FastCGI requests
var app = new FCGIApplication();
            
// Handle requests by responding with a 'Hello World' message
app.OnRequestReceived += (sender, request) => {
    request.WriteResponseASCII("Content-Type:text/html\n\nHello World!");
    request.Close();
};
// Start listening on port 19000
app.Run(19000);

// You now need a webserver like nginx or Apache to pass incoming requests
// via FastCGI to your application.
```


**Methods**

<a id="FastCGI.FCGIApplication.Listen(System.Int32)"></a>

* *void* **Listen** *(int port)*  
  Starts listening for connections on the given port.  
  Will only accept connections from localhost. Use the [Listen](#FastCGI.FCGIApplication.Listen(System.Net.IPEndPoint)) overload of this method to specify where to listen for connection.

<a id="FastCGI.FCGIApplication.Listen(System.Net.IPEndPoint)"></a>

* *void* **Listen** *(System.Net.IPEndPoint endPoint)*  
  Starts listening for connections on the given IP end point.  

<a id="FastCGI.FCGIApplication.Process"></a>

* *bool* **Process** *()*  
  Processes all data available on the current FastCGI connection and handles the received data.  
  Call this repeatedly to process incoming requests.
Alternatively, you can call [Run](#FastCGI.FCGIApplication.Run(System.Int32)) once, which will call [Listen](#FastCGI.FCGIApplication.Listen(System.Int32)) and execute this method in an infinite loop.
Internally, this method manages reconnections on the network socket and calls [ProcessSingleRecord](#FastCGI.FCGIApplication.ProcessSingleRecord(System.IO.Stream,System.IO.Stream)) .
Returns true if a record was read, false otherwise.

<a id="FastCGI.FCGIApplication.ProcessStream(System.IO.Stream,System.IO.Stream)"></a>

* *bool* **ProcessStream** *(Stream inputStream, Stream outputStream)*  
  Reads and handles all [Records](#FastCGI.Record) available on the custom inputStream and writes responses to outputStream.  
  Use [Process](#FastCGI.FCGIApplication.Process) if you don't need a custom stream, but instead want to process the records on the current FastCGI connection.
Returns true if a record was read, false otherwise.

<a id="FastCGI.FCGIApplication.ProcessSingleRecord(System.IO.Stream,System.IO.Stream)"></a>

* *bool* **ProcessSingleRecord** *(Stream inputStream, Stream outputStream)*  
  Tries to read and handle a [Record](#FastCGI.Record) from inputStream and writes responses to outputStream.  
  Use [ProcessStream](#FastCGI.FCGIApplication.ProcessStream(System.IO.Stream,System.IO.Stream)) to process all records on a stream.
Returns true if a record was read, false otherwise.

<a id="FastCGI.FCGIApplication.StopListening"></a>

* *void* **StopListening** *()*  
  Stops listening for incoming connections.  

<a id="FastCGI.FCGIApplication.Run(System.Int32)"></a>

* *void* **Run** *(int port)*  
  This method never returns! Starts listening for FastCGI requests on the given port.  
  Use [OnRequestReceived](#FastCGI.FCGIApplication.OnRequestReceived) to react to incoming requests.
Internally, this simply calls [Listen](#FastCGI.FCGIApplication.Listen(System.Int32)) and enters an infinite loop of [Process](#FastCGI.FCGIApplication.Process) calls.


**Events**

<a id="FastCGI.FCGIApplication.OnRequestReceived"></a>

* **OnRequestReceived**  
  Will be called whenever a request has been received.  
  Please note that multiple requests can be open at the same time.
This means that this event may fire multiple times before you call [Request.Close](#FastCGI.Request.Close) on the first one.



**Properties and Fields**

<a id="FastCGI.FCGIApplication.Connected"></a>

* *bool* **Connected**  
  True iff this application is currently connected to a webserver.  


<a id="FastCGI.FCGIApplication.Timeout"></a>

* *int* **Timeout**  
  The read/write timeouts in miliseconds for the listening socket, the connections, and the streams.  



<a id="FastCGI.FCGIApplication.OpenRequests"></a>

* *Dictionary&lt;int, FastCGI.Request&gt;* **OpenRequests**  
  A dictionary of all open [Requests](#FastCGI.Request) , indexed by the FastCGI request id.  


<a id="FastCGI.FCGIApplication.CurrentStream"></a>

* *Stream* **CurrentStream**  
  The network stream of the connection to the webserver.  
  Can be null if the application is currently not connected to a webserver.


<a id="FastCGI.FCGIApplication.RequestFinished"></a>

* *bool* **RequestFinished**  
  True iff this application is about to close the connection to the webserver.  





---

<a id="FastCGI.Record"></a>
## class FastCGI.Record

A FastCGI Record.

See section 3.3 of the FastCGI Specification for details.

**Methods**

<a id="FastCGI.Record.GetNameValuePairs"></a>

* *Dictionary&lt;string, Byte[]&gt;* **GetNameValuePairs** *()*  

<a id="FastCGI.Record.SetNameValuePairs(System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Byte[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]])"></a>

* *void* **SetNameValuePairs** *(Dictionary&lt;string, Byte[]&gt; nameValuePairs)*  

<a id="FastCGI.Record.WriteToStream(System.IO.Stream)"></a>

* *int* **WriteToStream** *(Stream stream)*  
  Writes this record to the given stream.  

<a id="FastCGI.Record.Send(System.IO.Stream)"></a>

* *void* **Send** *(Stream stream)*  
  Used internally. Writes the record to the given stream. Used for sending records to the webserver.  

<a id="FastCGI.Record.ToString"></a>

* *string* **ToString** *()*  

<a id="FastCGI.Record.Equals(System.Object)"></a>

* *bool* **Equals** *(Object obj)*  


**Properties and Fields**


<a id="FastCGI.Record.Version"></a>

* *byte* **Version**  
  The version byte. Should always equal [Constants.FCGI_VERSION_1](#FastCGI.Constants.FCGI_VERSION_1) .  


<a id="FastCGI.Record.Type"></a>

* *FastCGI.Record.RecordType* **Type**  
  The [RecordType](#FastCGI.Record.RecordType) of this record.  


<a id="FastCGI.Record.RequestId"></a>

* *int* **RequestId**  
  The request id associated with this record.  


<a id="FastCGI.Record.ContentLength"></a>

* *int* **ContentLength**  
  The length of [ContentData](#FastCGI.Record.ContentData) .  


<a id="FastCGI.Record.ContentData"></a>

* *Byte[]* **ContentData**  
  The data contained in this record.  



**Static Methods**

<a id="FastCGI.Record.ReadNameValuePairs(System.IO.Stream)"></a>

* *Dictionary&lt;string, Byte[]&gt;* **ReadNameValuePairs** *(Stream stream)*  
  Tries to read a dictionary of name-value pairs from the given stream  
  This method does not make any attempt to make sure whether this record actually contains a set of name-value pairs.
It will return nonsense or throw an EndOfStreamException if the record content does not contain valid name-value pairs.

<a id="FastCGI.Record.ReadVarLength(System.IO.Stream)"></a>

* *uint* **ReadVarLength** *(Stream stream)*  
  Reads a length from the given stream, which is encoded in one or four bytes.  
  See section 3.4 of the FastCGI specification for details.

<a id="FastCGI.Record.WriteVarLength(System.IO.Stream,System.UInt32)"></a>

* *void* **WriteVarLength** *(Stream stream, uint len)*  
  Writes a length from the given stream, which is encoded in one or four bytes.  
  See section 3.4 of the FastCGI specification for details.

<a id="FastCGI.Record.ReadInt16(System.IO.Stream)"></a>

* *ushort* **ReadInt16** *(Stream stream)*  
  Reads a 16-bit integer from the given stream.  

<a id="FastCGI.Record.WriteInt16(System.IO.Stream,System.UInt16)"></a>

* *void* **WriteInt16** *(Stream stream, ushort v)*  
  Writes a 16-bit integer to the given stream.  

<a id="FastCGI.Record.ReadRecord(System.IO.Stream)"></a>

* *FastCGI.Record* **ReadRecord** *(Stream stream)*  
  Reads a single Record from the given stream.  
  Returns the retreived record or null if no record could be read.
Will block if a partial record is on the stream, until the full record has arrived or a timeout occurs.

<a id="FastCGI.Record.CreateStdout(System.Byte[],System.Int32)"></a>

* *FastCGI.Record* **CreateStdout** *(Byte[] data, int requestId)*  
  Creates a Stdout record from the given data and request id  

<a id="FastCGI.Record.CreateEndRequest(System.Int32)"></a>

* *FastCGI.Record* **CreateEndRequest** *(int requestId)*  
  Creates a EndRequest record with the given request id  

<a id="FastCGI.Record.CreateGetValuesResult(System.Int32,System.Int32,System.Boolean)"></a>

* *FastCGI.Record* **CreateGetValuesResult** *(int maxConnections, int maxRequests, bool multiplexing)*  
  Creates a GetValuesResult record from the given config values.  




---

<a id="FastCGI.Request"></a>
## class FastCGI.Request

A FastCGI request.

A request usually corresponds to a HTTP request that has been received by the webserver (see the [FastCGI specification](http://www.fastcgi.com/devkit/doc/fcgi-spec.html) for details).

You will probably want to use [WriteResponse](#FastCGI.Request.WriteResponse(System.Byte[])) or its helper methods to output a response and then call [Close](#FastCGI.Request.Close) . Use [FCGIApplication.OnRequestReceived](#FastCGI.FCGIApplication.OnRequestReceived) to be notified of new requests.

Remember to call [Close](#FastCGI.Request.Close) when you wrote the complete response.

**Methods**

<a id="FastCGI.Request.GetParameterASCII(System.String)"></a>

* *string* **GetParameterASCII** *(string name)*  
  Returns the parameter with the given name as an ASCII encoded string.  

<a id="FastCGI.Request.GetParameterUTF8(System.String)"></a>

* *string* **GetParameterUTF8** *(string name)*  
  Returns the parameter with the given name as an UTF-8 encoded string.  

<a id="FastCGI.Request.WriteResponse(System.Byte[])"></a>

* *void* **WriteResponse** *(Byte[] data)*  
  Appends data to the response body.  
  The given data will be sent immediately to the webserver as a single stdout record.

<a id="FastCGI.Request.WriteResponseASCII(System.String)"></a>

* *void* **WriteResponseASCII** *(string data)*  
  Appends an ASCII string to the response body.  
  This is a helper function, it converts the given string to ASCII bytes and feeds it to [WriteResponse](#FastCGI.Request.WriteResponse(System.Byte[])) .

<a id="FastCGI.Request.WriteResponseUtf8(System.String)"></a>

* *void* **WriteResponseUtf8** *(string data)*  
  Appends an UTF-8 string to the response body.  
  This is a helper function, it converts the given string to UTF-8 bytes and feeds it to [WriteResponse](#FastCGI.Request.WriteResponse(System.Byte[])) .

<a id="FastCGI.Request.Close"></a>

* *void* **Close** *()*  
  Closes this request.  


**Properties and Fields**

<a id="FastCGI.Request.RequestId"></a>

* *int* **RequestId**  
  The id for this request, issued by the webserver  


<a id="FastCGI.Request.Body"></a>

* *string* **Body**  
  The request body.  
  For POST requests, this will contain the POST variables. For GET requests, this will be empty.



<a id="FastCGI.Request.Parameters"></a>

* *Dictionary&lt;string, Byte[]&gt;* **Parameters**  
  The FastCGI parameters received by the webserver, in raw byte arrays.  
  Use [GetParameterASCII](#FastCGI.Request.GetParameterASCII(System.String)) and [GetParameterUTF8](#FastCGI.Request.GetParameterUTF8(System.String)) to get strings instead of byte arrays.





---

