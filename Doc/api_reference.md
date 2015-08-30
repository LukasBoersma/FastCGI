<a id="FastCGI.FCGIApplication"></a>
## FCGIApplication
*class FastCGI.FCGIApplication*

Main FastCGI listener class.

This class manages a connection to a webserver by listening on a given port on localhost and receiving FastCGI requests by a webserver like Apache or nginx.
Call [FastCGI.FCGIApplication.Run(System.Int32)](#FastCGI.FCGIApplication.Run(System.Int32)) to start listening, and use [FastCGI.FCGIApplication.OnRequestReceived](#FastCGI.FCGIApplication.OnRequestReceived) to get notified of received requests.

In FastCGI terms, this class implements the responder role. Refer to section 6.2 of the FastCGI specification for details.

See the below example to learn how to accept requests. For more complex usage. have a look at the [FastCGI.Request](#FastCGI.Request) class.
If you need to go even deeper, see the [FastCGI.Record](#FastCGI.Record) class and read the FastCGI specification: http://www.fastcgi.com/devkit/doc/fcgi-spec.html

**Examples**


```csharp
// Create a new FCGIApplication, will accept FastCGI requests
  var app = new FCGIApplication();
            
  // Handle requests by responding with a 'Hello World' message
  app.OnRequestReceived += (sender, request) => {
      request.WriteBodyASCII("Content-Type:text/html\n\nHello World!");
      request.Close();
  };
  // Start listening on port 19000
  app.Run(19000);
```


**Methods**

<a id="FastCGI.FCGIApplication.Run(System.Int32)"></a>

* *void* **Run** *(Int32 port)*  
  This method never returns! Starts listening for FastCGI requests on the given port.  


**Events**

<a id="FastCGI.FCGIApplication.OnRequestReceived"></a>

* **OnRequestReceived**  
  Will be called whenever a request has been received.  
  Please note that multiple requests can be open at the same time.
This means that this event may fire multiple times before you call [FastCGI.Request.Close](#FastCGI.Request.Close) on the first one.



**Properties and Fields**

<a id="FastCGI.FCGIApplication.Connected"></a>

* *Boolean* **Connected**  
  True iff this application is currently connected to a webserver.  



<a id="FastCGI.FCGIApplication.OpenRequests"></a>

* *Dictionary&lt;Int32, Request&gt;* **OpenRequests**  
  A dictionary of all open [FastCGI.Request](#FastCGI.Request) , indexed by id.  


<a id="FastCGI.FCGIApplication.CurrentStream"></a>

* *Stream* **CurrentStream**  
  The network stream of the connection to the webserver.  
  Can be null if the application is currently not connected to a webserver.


<a id="FastCGI.FCGIApplication.Disconnecting"></a>

* *Boolean* **Disconnecting**  
  True iff this application is about to close the connection to the webserver.  





---

<a id="FastCGI.Record"></a>
## Record
*class FastCGI.Record*

A FastCGI Record.

See section 3.3 of the FastCGI Specification for details.

**Methods**

<a id="FastCGI.Record.GetNameValuePairs"></a>

* *Dictionary&lt;String, Byte[]&gt;* **GetNameValuePairs** *()*  
  Tries to read a dictionary of name-value pairs from the record content.  
  This method does not make any attempt to make sure whether this record actually contains a set of name-value pairs.
It will return nonsense or throw an EndOfStreamException if the record content does not contain valid name-value pairs.

<a id="FastCGI.Record.SetNameValuePairs(System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Byte[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]])"></a>

* *void* **SetNameValuePairs** *(Dictionary&lt;String, Byte[]&gt; nameValuePairs)*  

<a id="FastCGI.Record.WriteToStream(System.IO.Stream)"></a>

* *Int32* **WriteToStream** *(Stream stream)*  
  Writes this record to the given stream.  

<a id="FastCGI.Record.ToString"></a>

* *String* **ToString** *()*  


**Properties and Fields**


<a id="FastCGI.Record.Version"></a>

* *Byte* **Version**  
  The version byte. Should always equal [FastCGI.Constants.FCGI_VERSION_1](#FastCGI.Constants.FCGI_VERSION_1) .  


<a id="FastCGI.Record.Type"></a>

* *RecordType* **Type**  
  The [FastCGI.Record.RecordType](#FastCGI.Record.RecordType) of this record.  


<a id="FastCGI.Record.RequestId"></a>

* *Int32* **RequestId**  
  The request id associated with this record.  


<a id="FastCGI.Record.ContentLength"></a>

* *Int32* **ContentLength**  
  The length of [FastCGI.Record.ContentData](#FastCGI.Record.ContentData) .  


<a id="FastCGI.Record.ContentData"></a>

* *Byte[]* **ContentData**  
  The data contained in this record.  



**Static Methods**

<a id="FastCGI.Record.ReadRecord(System.IO.Stream)"></a>

* *Record* **ReadRecord** *(Stream stream)*  
  Reads a single Record from the given stream.  

<a id="FastCGI.Record.CreateStdout(System.Byte[],System.Int32)"></a>

* *Record* **CreateStdout** *(Byte[] data, Int32 requestId)*  
  Creates a Stdout record from the given data and request id  

<a id="FastCGI.Record.CreateEndRequest(System.Int32)"></a>

* *Record* **CreateEndRequest** *(Int32 requestId)*  
  Creates a EndRequest record with the given request id  

<a id="FastCGI.Record.CreateGetValuesResult(System.Int32,System.Int32,System.Boolean)"></a>

* *Record* **CreateGetValuesResult** *(Int32 maxConnections, Int32 maxRequests, Boolean multiplexing)*  
  Creates a GetValuesResult record from the given config values.  




---

<a id="FastCGI.Request"></a>
## Request
*class FastCGI.Request*

A FastCGI request.

A request usually corresponds to a HTTP request that has been received by the webserver.

You will probably want to use [FastCGI.Request.WriteResponse(System.Byte[])](#FastCGI.Request.WriteResponse(System.Byte[])) or its helper methods to output a response and then call [FastCGI.Request.Close](#FastCGI.Request.Close) . Use [FastCGI.FCGIApplication.OnRequestReceived](#FastCGI.FCGIApplication.OnRequestReceived) to be notified of new requests.

Refer to the FastCGI specification for more details.

**Methods**

<a id="FastCGI.Request.WriteResponse(System.Byte[])"></a>

* *void* **WriteResponse** *(Byte[] data)*  
  Appends data to the response body.  
  The given data will be sent immediately to the webserver as a single stdout record.

<a id="FastCGI.Request.WriteResponseASCII(System.String)"></a>

* *void* **WriteResponseASCII** *(String data)*  
  Appends an ASCII string to the response body.  
  This is a helper function, it converts the given string to ASCII bytes and feeds it to [FastCGI.Request.WriteResponse(System.Byte[])](#FastCGI.Request.WriteResponse(System.Byte[])) .

<a id="FastCGI.Request.WriteResponseUtf8(System.String)"></a>

* *void* **WriteResponseUtf8** *(String data)*  
  Appends an UTF-8 string to the response body.  
  This is a helper function, it converts the given string to UTF-8 bytes and feeds it to [FastCGI.Request.WriteResponse(System.Byte[])](#FastCGI.Request.WriteResponse(System.Byte[])) .

<a id="FastCGI.Request.Close"></a>

* *void* **Close** *()*  
  Closes this request.  


**Properties and Fields**

<a id="FastCGI.Request.RequestId"></a>

* *Int32* **RequestId**  
  The id for this request, issued by the webserver  


<a id="FastCGI.Request.Body"></a>

* *String* **Body**  
  The request body.  
  For POST requests, this will contain the POST variables. For GET requests, this will be empty.



<a id="FastCGI.Request.Parameters"></a>

* *Dictionary&lt;String, String&gt;* **Parameters**  
  The FastCGI parameters passed by the webserver.  
  All strings are encoded in ASCII, regardless of any encoding information in the request.





---

