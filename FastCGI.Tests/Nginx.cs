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
using System.Reflection;

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
                ChildProcessTracker.AddProcess(nginxProcess);
            }
            catch(Win32Exception exception)
            {
                if(nginxProcess != null && !nginxProcess.HasExited)
                    nginxProcess.Kill();
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
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.CurrentDirectory = baseDir;

            Assert.IsTrue(File.Exists(configFile), "Config file should exist");

            Directory.CreateDirectory("logs");
            Directory.CreateDirectory("temp");

            var nginxProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "nginx",
                Arguments = "-c " + configFile + " -p ./ -g \"error_log logs/error.log;\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            ChildProcessTracker.AddProcess(nginxProcess);

            Thread.Sleep(5000);

            if (nginxProcess.HasExited)
            {
                var nginxOutput = nginxProcess.StandardOutput.ReadToEnd() + "\n====STDERR===\n" + nginxProcess.StandardError.ReadToEnd();
                Console.WriteLine("Nginx exited with output:");
                Console.WriteLine(nginxOutput);
                File.WriteAllText("nginx_output.txt", nginxOutput);
            }

            Assert.IsFalse(nginxProcess.HasExited, "nginx process should be running");
            
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
                string result = null;
                try
                {
                    WebResponse response = request.GetResponse();
                    var reader = new StreamReader(response.GetResponseStream());
                    result = reader.ReadToEnd();
                }
                catch(WebException e)
                {
                    // ignore timeouts, just return null
                }
                return result;
            });
        }

        /// <summary>
        /// Start a Nginx server that is connected to a FCGIApplication and do a single HTTP request.
        /// </summary>
        [Test, NonParallelizable, Explicit]
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
        [Test, NonParallelizable, Explicit]
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

            Task<string>[] results = new Task<string>[500];
            // Count the successful responses
            int successCount = 0;
            // We will allow up to 5% failure rate
            int minimumSuccessCount = (int)(results.Length * 0.95);

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = GetHttpAsync("http://localhost:8182");
                Thread.Sleep(20);
            }

            for (int i = 0; i < results.Length; i++)
            {
                results[i].Wait(20000);
                var result = results[i].Result;
                if(expectedResult == result)
                {
                    successCount++;
                }
            }

            Assert.GreaterOrEqual(successCount, minimumSuccessCount, "At least 95% of requests should be successful");

            StopNginx(nginx);
            app.Stop();
            appThread.Join(500);
        }
        /// <summary>
        /// Same as <see cref="Nginx_ManyRequests"/>, but with Nginx configured to keep the FastCGI connections open after requests.
        /// </summary>
        [Test, NonParallelizable, Explicit]
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

            Task<string>[] results = new Task<string>[500];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = GetHttpAsync("http://localhost:8182");
                Thread.Sleep(20);
            }

            // Count the number of correct results
            int successCount = 0;
            // We will allow 2% of errors
            int minimumSuccessCount = (int)(results.Length * 0.95);

            for (int i = 0; i < results.Length; i++)
            {
                results[i].Wait(20000);
                
                var result = results[i].Result;
                if (result == expectedResult)
                    successCount++;
            }

            Assert.GreaterOrEqual(successCount, minimumSuccessCount, "At least 95% of requests should be successful");

            StopNginx(nginx);
            app.Stop();
            appThread.Join(500);
        }


    }
}
