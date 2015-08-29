# FastCGI for .NET

[![Build Status](https://travis-ci.org/LukasBoersma/FastCGI.svg)](https://travis-ci.org/LukasBoersma/FastCGI)

This is an implementation of [FastCGI](http://www.fastcgi.com/devkit/doc/fcgi-spec.html) for .NET, written in C#. It implements the parts of the protocol that are necessary to build a simple web application using .NET.

This means that you can write web applications in C# that serve dynamic content.

## License and contributing

This software is distributed under the terms of the MIT license. You can use it for your own projects for free under the conditions specified in LICENSE.txt. 

If you have questions, feel free to contact me. Visit [lukas-boersma.com](https://lukas-boersma.com) for my contact details.

If you think you found a bug, you can open an Issue on Github. If you make changes to this library, I would be happy about a pull request.

## Documentation

I am hosting the full API documentation here: [lukas-boersma.com/fastcgi-docs](https://lukas-boersma.com/fastcgi-docs/).

## Nuget

The library is available via [NuGet](https://www.nuget.org/packages/FastCGI/). To install, type this in the package manager console:

```
    Install-Package FastCGI
```

## Basic usage

The most common usage scenario is to use this library together with a web server like Apache and nginx. The web server will serve static content and forward HTTP requests for dynamic content to your application.

Have a look at the FastCGI.FCGIApplication class for usage examples and more information.

This code example shows how to create a FastCGI application and receive requests:

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

## Web server configuration

Refer to your web server documentation for configuration details:

 * [nginx documentation](http://nginx.org/en/docs/http/ngx_http_fastcgi_module.html)
 * [Apache documentation](http://httpd.apache.org/mod_fcgid/mod/mod_fcgid.html)

For nginx, add this to pass all requests to your FastCGI application:

    location / {
        include fastcgi_params;
        fastcgi_pass   127.0.0.1:19000;
    }

Where fastcgi_params is a file in your nginx config folder, containing something like:

    fastcgi_param   QUERY_STRING            $query_string;
    fastcgi_param   REQUEST_METHOD          $request_method;
    fastcgi_param   CONTENT_TYPE            $content_type;
    fastcgi_param   CONTENT_LENGTH          $content_length;

    fastcgi_param   SCRIPT_FILENAME         $document_root$fastcgi_script_name;
    fastcgi_param   SCRIPT_NAME             $fastcgi_script_name;
    fastcgi_param   PATH_INFO               $fastcgi_path_info;
    fastcgi_param 	PATH_TRANSLATED         $document_root$fastcgi_path_info;
    fastcgi_param   REQUEST_URI             $request_uri;
    fastcgi_param   DOCUMENT_URI            $document_uri;
    fastcgi_param   DOCUMENT_ROOT           $document_root;
    fastcgi_param   SERVER_PROTOCOL         $server_protocol;

    fastcgi_param   GATEWAY_INTERFACE       CGI/1.1;
    fastcgi_param   SERVER_SOFTWARE         nginx/$nginx_version;

    fastcgi_param   REMOTE_ADDR             $remote_addr;
    fastcgi_param   REMOTE_PORT             $remote_port;
    fastcgi_param   SERVER_ADDR             $server_addr;
    fastcgi_param   SERVER_PORT             $server_port;
    fastcgi_param   SERVER_NAME             $server_name;

    fastcgi_param   HTTPS                   $https;
