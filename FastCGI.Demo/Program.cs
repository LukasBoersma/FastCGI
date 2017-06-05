using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FastCGI;

namespace FastCGI.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a new FCGIApplication, will accept FastCGI requests
            var app = new FCGIApplication();

            // Handle requests by responding with a 'Hello World' message
            app.OnRequestReceived += (sender, request) =>
            {
                var responseString =
                      "HTTP/1.1 200 OK\n"
                    + "Content-Type:text/html\n"
                    + "\n"
                    + "Hello World!";

                request.WriteResponseASCII(responseString);
                request.Close();
            };

            // Start listening on port 19000
            app.Run(19000);
        }
    }
}
