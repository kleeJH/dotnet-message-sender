using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace SocketSyncFile
{
    /// <summary>
    /// A socket that listens to input streams.
    /// It tries to get a file (i.e. .txt or .zip) from the established connection, stores the file into a folder and sends it back to the sender.
    /// </summary>
    class Program
    {
        static readonly int Port = 11001;

        static void Main(string[] args)
        {
            ExecuteServer();
            Console.ReadLine();
        }

        private static bool SocketConnected(Socket socket)
        {
            return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0) && socket.Connected;
        }

        public static void ExecuteServer()
        {
            try
            {
                IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, Port);

                Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Console.WriteLine("Listening at [" + ipAddr + "]:" + Port);

                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    Console.WriteLine("[Socket] Waiting for a connection... ");

                    Socket clientSocket = listener.Accept();

                    while (SocketConnected(clientSocket))
                    {
                        Console.WriteLine("[Socket] Waiting for the message to be sent... ");

                        // Initializing buffer and storage for input stream
                        byte[] bytes = new byte[1024];
                        MemoryStream requestStream = new MemoryStream();

                        try
                        {
                            // Reading 1MB buffer from socket
                            do
                            {
                                bytes = new byte[1024];
                                int numByte = clientSocket.Receive(bytes);
                                requestStream.Write(bytes, 0, numByte);
                            } while (clientSocket.Available > 0);
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine("[Socket] Client socket disconnected...");
                            break;
                        }

                        byte[] data = requestStream.ToArray();

                        // Save into a directory
                        string savePath = Directory.GetCurrentDirectory() + @"\Retrieved\";
                        if (!Directory.Exists(savePath))
                        {
                            Directory.CreateDirectory(savePath);
                        }

                        string fileName = "";

                        // Check if file is .zip
                        if (data.Length >= 4 && data[0] == 0x50 && data[1] == 0x4b && data[2] == 0x03 && data[3] == 0x04)
                        {
                            fileName = $"{DateTime.Now.ToString("dd-MM-yyyy HH.mm.ss")}.zip";
                        }
                        else
                        {
                            fileName = $"{DateTime.Now.ToString("dd-MM-yyyy HH.mm.ss")}.txt";
                        }

                        // Save data into new file
                        using (var fs = new FileStream(savePath + fileName, FileMode.Create))
                        {
                            fs.Write(data, 0, data.Length);
                        }

                         Console.WriteLine("[Socket] Received :: \"{0}\". The server has read {1} bytes.", Encoding.ASCII.GetString(data), data.Length);

                        clientSocket.Send(data);
                    }

                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
