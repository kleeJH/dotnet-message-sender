using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace SocketSyncMessage
{
    /// <summary>
    /// A socket that listens to input streams.
    /// It tries to get a string from the established connection and sends it back to the sender.
    /// </summary>
    class Program
    {
        static readonly int Port = 11000;

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

                        // Initializing buffer
                        byte[] bytes = new byte[1024];
                        string data = "";

                        try
                        {
                            // Reading 1MB buffer from socket
                            do
                            {
                                bytes = new byte[1024];
                                int numByte = clientSocket.Receive(bytes);
                                data += Encoding.ASCII.GetString(bytes,
                                    0, numByte);
                            } while (clientSocket.Available > 0);
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine("[Socket] Client socket disconnected...");
                            break;
                        }

                        Console.WriteLine("[Socket] Received :: \"{0}\". The server has read {1} bytes.", data, data.Length);
                        byte[] message = Encoding.ASCII.GetBytes(data);

                        clientSocket.Send(message);
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
