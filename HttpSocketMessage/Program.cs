using System;
using System.IO;
using System.Net;
using System.Text;

namespace HttpSocketMessage
{
    class Program
    {
        private const string DEFAULT_SCHEME = "http", DEFAULT_HOST = "localhost";
        private const Int32 DEFAULT_PORT = 12000;

        static void Main(string[] args)
        {
            StartHttpMessageListener();
        }

        static void StartHttpMessageListener()
        {
            using (var listener = new HttpListener())
            {
                // Define Uri
                UriBuilder builder = new UriBuilder
                {
                    Scheme = DEFAULT_SCHEME,
                    Host = DEFAULT_HOST,
                    Port = DEFAULT_PORT
                };
                listener.Prefixes.Add(builder.Uri.ToString());

                listener.Start();
                Console.WriteLine($"Listening at [{builder.Uri}]");

                while (true)
                {
                    // Waits for incoming requests
                    Console.WriteLine("\nWaiting for HTTP request...");
                    HttpListenerContext ctx = listener.GetContext(); // Waiting...

                    // Request
                    HttpListenerRequest req = ctx.Request;
                    Console.WriteLine($"Received request for {req.Url}");
                    Console.WriteLine($"Request UserAgent :: {req.Headers.Get("UserAgent")}"); // User-Agent

                    StreamReader sr = new StreamReader(req.InputStream);
                    string reqBody = sr.ReadToEnd();
                    Console.WriteLine($"Request Body :: {reqBody}");

                    // Response
                    using (HttpListenerResponse resp = ctx.Response)
                    {
                        // Status
                        resp.StatusCode = (int)HttpStatusCode.OK;
                        resp.StatusDescription = "Status OK";

                        // Respond with plain text
                        resp.Headers.Set("Content-Type", "text-plain");

                        Console.WriteLine("Sent response :: " + reqBody);
                        byte[] buffer = Encoding.UTF8.GetBytes(reqBody);
                        resp.ContentLength64 = buffer.Length;

                        using (Stream ros = resp.OutputStream)
                        {
                            ros.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
        }
    }
}
