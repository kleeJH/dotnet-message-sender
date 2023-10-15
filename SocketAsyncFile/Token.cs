using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SocketAsyncFile
{
    delegate void ProcessData(SocketAsyncEventArgs args);

    /// <summary>
    /// Token for use with SocketAsyncEventArgs.
    /// </summary>
    internal sealed class Token : IDisposable
    {
        private Socket connection;

        private MemoryStream reqStream = new MemoryStream();

        private readonly Int32 bufferSize;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="connection">Socket to accept incoming data.</param>
        /// <param name="bufferSize">Buffer size for accepted data.</param>
        internal Token(Socket connection, Int32 bufferSize)
        {
            this.connection = connection;
            this.bufferSize = bufferSize;
        }

        /// <summary>
        /// Accept socket.
        /// </summary>
        internal Socket Connection
        {
            get { return this.connection; }
        }

        /// <summary>
        /// Process data received from the client.
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs used in the operation.</param>
        internal void ProcessData(SocketAsyncEventArgs args)
        {
            // Get the byte[] received from the client.
            byte[] received = reqStream.ToArray();

            //TODO :: Use message received to perform a specific operation.
            Console.WriteLine("[Socket] Received :: \"{0}\". The server has read {1} bytes.", Encoding.ASCII.GetString(received), received.Length);

            // Save into a directory
            string savePath = Directory.GetCurrentDirectory() + @"\Retrieved\";
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            string fileName;

            // Check if file is .zip
            if (received.Length >= 4 && received[0] == 0x50 && received[1] == 0x4b && received[2] == 0x03 && received[3] == 0x04)
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
                fs.Write(received, 0, received.Length);
            }

            // Prepare response
            Byte[] sendBuffer = received;
            args.SetBuffer(sendBuffer, 0, sendBuffer.Length);

            reqStream = new MemoryStream();
        }

        /// <summary>
        /// Set data received from the client.
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs used in the operation.</param>
        internal void SetData(SocketAsyncEventArgs args)
        {
            Int32 count = args.BytesTransferred;

            reqStream.Write(args.Buffer, 0, count);
        }

        #region IDisposable Members

        /// <summary>
        /// Release instance.
        /// </summary>
        public void Dispose()
        {
            try
            {
                this.connection.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {
                // Throw if client has closed, so it is not necessary to catch.
            }
            finally
            {
                this.connection.Close();
            }
        }

        #endregion
    }
}
