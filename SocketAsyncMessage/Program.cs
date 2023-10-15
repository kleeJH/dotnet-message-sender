using System;

namespace SocketAsyncMessage
{
    /// <summary>
    /// A socket that listens to input streams asynchronously.
    /// It tries to get a string from the established connection and sends it back to the sender.
    /// </summary>
    public static class Program
    {
        private const Int32 DEFAULT_PORT = 11002, DEFAULT_NUM_CONNECTIONS = 10, DEFAULT_BUFFER_SIZE = Int16.MaxValue;

        public static void Main(String[] args)
        {
            try
            {
                Int32 port = GetValue(args, 0, DEFAULT_PORT);
                Int32 numConnections = GetValue(args, 1, DEFAULT_NUM_CONNECTIONS);
                Int32 bufferSize = GetValue(args, 2, DEFAULT_BUFFER_SIZE);

                SocketListener sl = new SocketListener(numConnections, bufferSize);
                string ip = sl.Start(port);

                Console.WriteLine("Server listening on [{0}]:{1}. Press any key to terminate the server process...", ip, port);
                Console.Read();

                sl.Stop();

            }
            catch (IndexOutOfRangeException)
            {
                PrintUsage();
            }
            catch (FormatException)
            {
                PrintUsage();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static Int32 GetValue(String[] args, Int32 index, Int32 defaultValue)
        {
            if (args.Length <= index || !Int32.TryParse(args[index], out Int32 value))
            {
                return defaultValue;
            }

            return value;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: SocketAsyncServer <port> [numConnections] [bufferSize].");
            Console.WriteLine("\t<port> Numeric value for the listening TCP port.");
            Console.WriteLine("\t[numConnections] Numeric value for the maximum number of incoming connections.");
            Console.WriteLine("\t[bufferSize] Numeric value for the buffer size of incoming connections.");
        }
    }
}
