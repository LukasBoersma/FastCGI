# FastCGI for .NET

This is an incomplete implementation of [FastCGI](http://www.fastcgi.com/devkit/doc/fcgi-spec.html) for .NET, written in C#.

It implements the parts of FastCGI that are neccessary to build a simple web application using .NET.

## Basic Usage

This code example shows how to create a FastCGI application and receive requests

    // Create a new FCGIApplication, will accept FastCGI requests
    var app = new FCGIApplication();
    
    // Handle requests by responding with a 'Hello World' message
    app.OnRequestReceived += (sender, request) => {
        request.WriteBodyASCII("Content-Type:text/html\n\nHello World!");
        request.Close();
    };

    // Start listening on port 19000
    app.Run(19000);

## HTTP server configuration

Refer to your HTTP server documentation for configuration details:

 * [nginx documentation](http://nginx.org/en/docs/http/ngx_http_fastcgi_module.html)
 * [Apache documentation](http://httpd.apache.org/mod_fcgid/mod/mod_fcgid.html)

For nginx, add this to pass all requests to your FastCGI application:

    location / {
        fastcgi_pass   127.0.0.1:19000;
        include fastcgi_params
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
