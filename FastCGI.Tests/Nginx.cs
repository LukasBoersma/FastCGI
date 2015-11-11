using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

using NUnit.Framework;

using FastCGI;
using System.Net;
using System.IO;
using System.Threading;

namespace FastCGI.Tests
{
    [TestFixture]
    class Nginx
    {
        void AssertNginxInPath()
        {
            Process nginxProcess = null;

            // Try to run nginx with version output parameter
            try
            {
                nginxProcess = Process.Start(new ProcessStartInfo {
                    FileName = "nginx",
                    Arguments = "-v",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });
            }
            catch(Win32Exception exception)
            {
                // Throw an IgnoreException if nginx was not found
                Assert.Ignore("Unable to run Nginx server. Do you have nginx in your PATH? Skipping Nginx test.");
            }

            nginxProcess.WaitForExit(500);
            var nginxOutput = nginxProcess.StandardError.ReadToEnd();
            
            string versionPrefix = "nginx version: nginx/";
            if(!nginxOutput.StartsWith(versionPrefix))
                Assert.Ignore("Nginx server was found, but did not respond as expected. Skipping Nginx tests.");
        }

        Process StartNginx(string configFile)
        {
            Directory.CreateDirectory("logs");
            Directory.CreateDirectory("temp");
            var nginxProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "nginx",
                Arguments = "-c " + configFile,
            });

            Thread.Sleep(5000);

            return nginxProcess;
        }

        void StopNginx(Process nginxProcess)
        {
            nginxProcess.Kill();
            nginxProcess.Close();
        }

        static string GetHttp(string uri)
        {
            string result = null;

            WebRequest request = WebRequest.Create(uri);
            request.Timeout = 5000;
            WebResponse response = request.GetResponse();
            var reader = new StreamReader(response.GetResponseStream());
            result = reader.ReadToEnd();

            return result;
        }

        static async Task<string> GetHttpAsync(string uri)
        {
            return await Task.Run(() => {
                WebRequest request = WebRequest.Create(uri);
                request.Timeout = 5000;
                WebResponse response = request.GetResponse();
                var reader = new StreamReader(response.GetResponseStream());
                return reader.ReadToEnd();
            });
        }

        /// <summary>
        /// Start a Nginx server that is connected to a FCGIApplication and do a single HTTP request.
        /// </summary>
        [Test]
        public void Nginx_SingleRequest()
        {
            AssertNginxInPath();

            var nginx = StartNginx("ServerConfigs/NginxBasicConfig.conf");
            var expectedResult = "Hello World!";

            var app = new FCGIApplication();
            app.OnRequestReceived += (sender, request) =>
            {
                request.WriteResponseASCII("HTTP/1.1 200 OK\nContent-Type:text/html\n\n" + expectedResult);
                request.Close();
            };

            var appThread = new Thread(() => {
                app.Run(19000);
            });

            appThread.Start();

            var result = GetHttp("http://localhost:8182");

            app.Stop();
            StopNginx(nginx);

            Assert.AreEqual(expectedResult, result);
        }

        /// <summary>
        /// Start a Nginx server that is connected to a FCGIApplication and do a large amount of requests in a very short time and make sure that they are all handled correctly.
        /// </summary>
        [Test]
        public void Nginx_ManyRequests()
        {
            AssertNginxInPath();

            var nginx = StartNginx("ServerConfigs/NginxBasicConfig.conf");
            var expectedResult = "Hello World!";

            var app = new FCGIApplication();
            app.OnRequestReceived += (sender, request) =>
            {
                request.WriteResponseASCII("HTTP/1.1 200 OK\nContent-Type:text/html\n\n" + expectedResult);
                request.Close();
            };

            var appThread = new Thread(() => {
                app.Run(19000);
            });

            appThread.Start();

            Task<string>[] results = new Task<string>[1000];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = GetHttpAsync("http://localhost:8182");
                Thread.Sleep(1);
            }

            for (int i = 0; i < results.Length; i++)
            {
                results[i].Wait(20000);
                var result = results[i].Result;
                Assert.AreEqual(expectedResult, result);
            }

            StopNginx(nginx);
            app.Stop();
            appThread.Join(500);
        }
        /// <summary>
        /// Same as <see cref="Nginx_ManyRequests"/>, but with Nginx configured to keep the FastCGI connections open after requests.
        /// </summary>
        [Test]
        public void Nginx_ManyRequests_Keepalive()
        {
            AssertNginxInPath();

            var nginx = StartNginx("ServerConfigs/NginxKeepalive.conf");
            var expectedResult = "Hello World!";

            var app = new FCGIApplication();
            app.OnRequestReceived += (sender, request) =>
            {
                request.WriteResponseASCII("HTTP/1.1 200 OK\nContent-Type:text/html\n\n" + expectedResult);
                request.Close();
            };

            var appThread = new Thread(() => {
                app.Run(19000);
            });

            appThread.Start();

            Task<string>[] results = new Task<string>[1000];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = GetHttpAsync("http://localhost:8182");
                Thread.Sleep(1);
            }

            for (int i = 0; i < results.Length; i++)
            {
                results[i].Wait(20000);
                var result = results[i].Result;
                Assert.AreEqual(expectedResult, result);
            }

            StopNginx(nginx);
            app.Stop();
            appThread.Join(500);
        }


    }
}
