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
            app.OnRequestReceived += (sender, request) => {
                request.WriteResponseASCII("Content-Type:text/html\n\nHello World!");
                request.Close();
            };
            
            // Start listening on port 19000
            app.Run(19000);
        }
    }
}
