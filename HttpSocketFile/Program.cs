using System;
using System.IO;
using System.Net;
using System.Text;

namespace HttpSocketFile
{
    class Program
    {
        private const string DEFAULT_SCHEME = "http", DEFAULT_HOST = "localhost";
        private const Int32 DEFAULT_PORT = 12001;

        static void Main(string[] args)
        {
            StartHttpFileListener();
        }

        static void StartHttpFileListener()
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
                    Console.WriteLine("\nWaiting for http request...");
                    HttpListenerContext ctx = listener.GetContext(); // Waiting...

                    // Request
                    HttpListenerRequest req = ctx.Request;
                    Console.WriteLine($"Received request for {req.Url}");
                    Console.WriteLine($"Request User-Agent :: {req.Headers.Get("UserAgent")}"); // User-Agent

                    // Get data from request
                    MemoryStream sr = new MemoryStream();
                    req.InputStream.CopyTo(sr);
                    byte[] byteData = sr.ToArray();

                    Console.WriteLine($"Request Body :: {Encoding.ASCII.GetString(byteData)}");

                    // Save into a directory
                    string savePath = Directory.GetCurrentDirectory() + @"\Retrieved\";
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                    }

                    string fileName = "";
                    Console.WriteLine("ContentType :: " + req.ContentType);
                    switch (req.ContentType.ToString())
                    {
                        case "application/zip":
                            fileName = $"{DateTime.Now.ToString("dd-MM-yyyy HH.mm.ss")}-ZIP.zip";
                            break;
                        case "application/xml":
                            fileName = $"{DateTime.Now.ToString("dd-MM-yyyy HH.mm.ss")}-XML.txt";
                            break;
                        case "application/json":
                            fileName = $"{DateTime.Now.ToString("dd-MM-yyyy HH.mm.ss")}-JSON.txt";
                            break;
                        case "application/soap+xml":
                            fileName = $"{DateTime.Now.ToString("dd-MM-yyyy HH.mm.ss")}-SOAP.txt";
                            break;
                    }

                    // Save data into new file
                    using (var fs = new FileStream(savePath + fileName, FileMode.Create))
                    {
                        fs.Write(byteData, 0, byteData.Length);
                    }

                    // Response
                    using (HttpListenerResponse resp = ctx.Response)
                    {
                        // Status
                        resp.StatusCode = (int)HttpStatusCode.OK;
                        resp.StatusDescription = "Status OK";

                        // Respond with plain text
                        resp.Headers.Set("Content-Type", "text-plain");

                        Console.WriteLine("Sent response :: " + Encoding.ASCII.GetString(byteData));
                        resp.ContentLength64 = byteData.Length;

                        using (Stream ros = resp.OutputStream)
                        {
                            ros.Write(byteData, 0, byteData.Length);
                        }
                    }
                }
            }
        }
    }
}
