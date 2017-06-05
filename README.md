# FastCGI for .NET

[![Build Status](https://travis-ci.org/LukasBoersma/FastCGI.svg)](https://travis-ci.org/LukasBoersma/FastCGI)

This is an implementation of [FastCGI](http://www.fastcgi.com/devkit/doc/fcgi-spec.html) for .NET, written in C#. It implements the parts of the protocol that are necessary to build a simple web application using .NET.

This means that you can write web applications in C# that serve dynamic content.

## License and contributing

This software is distributed under the terms of the MIT license. You can use it for your own projects for free under the conditions specified in LICENSE.txt.

If you have questions, feel free to contact me. Visit [lukas-boersma.com](https://lukas-boersma.com) for my contact details.

If you think you found a bug, you can open an Issue on Github. If you make changes to this library, I would be happy about a pull request.

## Documentation

The project documentation is hosted here: [fastcgi-for-net.readthedocs.org](http://fastcgi-for-net.readthedocs.org/en/latest/)

The user guide is mostly the same as this readme. [Here is a direct link to the API reference](http://fastcgi-for-net.readthedocs.org/en/latest/api_reference).

## Nuget

The library is available via [NuGet](https://www.nuget.org/packages/FastCGI/). To install, type this in the package manager console:

    Install-Package FastCGI

Or, using the command-line interface:

    nuget install FastCGI

For more information, refer to the [NuGet documentation](https://docs.nuget.org/consume).

## Basic usage

The most common usage scenario is to use this library together with a web server like Apache and nginx. The web server will serve static content and forward HTTP requests for dynamic content to your application.

Have a look at the FastCGI.FCGIApplication class for usage examples and more information.

This code example shows how to create a FastCGI application and receive requests:

```csharp
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
```

## Web server configuration

For nginx, use `fastcgi_pass` to pass requests to your FastCGI application:

    location / {
        fastcgi_pass   127.0.0.1:19000; # Pass all requests to port 19000 via FastCGI.
        include fastcgi_params; # (Optional): Set several FastCGI parameters like the remote IP and other metadata.
    }

For more details, refer to your web server documentation for configuration details:

 * [nginx documentation](http://nginx.org/en/docs/http/ngx_http_fastcgi_module.html)
 * [Apache documentation](http://httpd.apache.org/mod_fcgid/mod/mod_fcgid.html)
