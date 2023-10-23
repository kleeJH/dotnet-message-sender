using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessageSender
{
    public class ConnectionPreparer
    {
        private readonly FormMain FormMain;
        public int Timeout { get; set; } = 500;
        public bool IsConnected { get; private set; } = false;
        public string Host { get { return ConnectionRequest.Host; } }
        public ushort Port { get { return ConnectionRequest.Port ?? 80; } }
        public string ObjectURL { get { return HttpInfo.HasValue ? HttpInfo.Value.ObjectPath : string.Empty; } }

        private ConnectionConstructor ConnectionRequest;
        private HttpWebRequest HttpRequest;

        private HttpInfo? HttpInfo;
        private AWSInfo? AWSInfo;

        private IPAddress IPAddr;
        private IPEndPoint IPEndpoint;
        private Socket SocketClient;
        private readonly ProtocolType PortType = ProtocolType.Tcp; // Currently only supports TCP

        internal ConnectionPreparer(FormMain formMain, ConnectionConstructor connInfo) // Socket, HttpSocket, Http
        {
            FormMain = formMain;
            ConstructConnectionPreparer(connInfo, null, null);
        }

        internal ConnectionPreparer(FormMain formMain, ConnectionConstructor connInfo, HttpInfo httpinfo) //HttpSocket, Http
        {
            FormMain = formMain;
            ConstructConnectionPreparer(connInfo, httpinfo, null);
        }

        internal ConnectionPreparer(FormMain formMain, ConnectionConstructor connInfo, AWSInfo awsinfo) // AWS
        {
            FormMain = formMain;
            ConstructConnectionPreparer(connInfo, null, awsinfo);
        }

        internal ConnectionPreparer(FormMain formMain, ConnectionConstructor connInfo, HttpInfo httpinfo, AWSInfo awsinfo) //AWS
        {
            FormMain = formMain;
            ConstructConnectionPreparer(connInfo, httpinfo, awsinfo);
        }

        /// <summary>
        /// Sends a request with no message and receive a ConnectionResponse object
        /// </summary>
        /// <returns>ConnectionResponse</returns>
        public ConnectionResponse Send()
        {
            Logging.Info($"[Send] :: Sending message via {ConnectionRequest.Type}");

            MemoryStream responseStream = new MemoryStream();

            HttpWebResponse response;
            SetConnectionTimeout(ConnectionRequest.Type);

            try
            {
                switch (ConnectionRequest.Type)
                {
                    case ConnectionType.Socket:
                        ConnectSocket();
                        SocketClient.Send(new byte[0]);

                        byte[] messageReceived = new byte[1024];
                        int byteRecv = 0;
                        SocketClient.ReceiveTimeout = 1000;

                        do
                        {
                            byteRecv = SocketClient.Receive(messageReceived);
                            responseStream.Write(messageReceived, 0, byteRecv);
                        } while (SocketClient.Available > 0);

                        SocketClient.Shutdown(SocketShutdown.Both);
                        SocketClient.Close();

                        return new ConnectionResponse(responseStream.ToArray());
                    case ConnectionType.HttpSocket:
                    case ConnectionType.Http:
                    case ConnectionType.AWS:
                        response = (HttpWebResponse)HttpRequest.GetResponse();
                        return new ConnectionResponse(response);
                }
            }
            catch (Exception ex)
            {
                FormMain.LogMessage(LogTypes.ERROR, $"[Send] :: [{ConnectionRequest.Type}] :: [Error]\n>>>\n{ex}\n<<<");
            }

            return null;
        }

        /// <summary>
        /// Sends a request with no message and receive a ConnectionResponse object
        /// </summary>
        /// <returns>Task<ConnectionResponse></returns>
        public async Task<ConnectionResponse> SendAsync()
        {
            Logging.Info($"[SendAsync] :: Sending message via {ConnectionRequest.Type}");
            byte[] socketResp;

            HttpWebResponse response;

            SetConnectionTimeout(ConnectionRequest.Type);

            try
            {
                switch (ConnectionRequest.Type)
                {
                    case ConnectionType.Socket:
                        using (SocketClient sa = new SocketClient(ConnectionRequest.Host, (int)ConnectionRequest.Port))
                        {
                            sa.Connect();
                            socketResp = sa.SendReceive(new byte[0], Timeout);
                            sa.Disconnect();
                            sa.Dispose();
                        }

                        return new ConnectionResponse(socketResp);

                    case ConnectionType.HttpSocket:
                    case ConnectionType.Http:
                    case ConnectionType.AWS:
                        response = (HttpWebResponse)await HttpRequest.GetResponseAsync();
                        return new ConnectionResponse(response);
                    default:
                        Logging.Error($"[SendAsync] :: Invalid ConnectionType, Request cannot be sent!");
                        break;
                }

            }
            catch (Exception ex)
            {
                FormMain.LogMessage(LogTypes.ERROR, $"[SendAsync] :: [{ConnectionRequest.Type}] :: [Error]\n>>>\n{ex}\n<<<");
            }
            return null;
        }

        /// <summary>
        /// Send a string message to the host and receive a ConnectionResponse object
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="messageFormat">Format of the message</param>
        /// <returns>ConnectionResponse</returns>
        public ConnectionResponse SendMessage(string message, MessageFormat messageFormat, string contentTypeStr)
        {
            Logging.Info($"[SendMessage] :: Sending message via {ConnectionRequest.Type}");
            Logging.Info($"[SendMessage] :: [Message To Be Sent]\n>>>\n{message}\n<<<");

            byte[] msgBytes = Encoding.ASCII.GetBytes(message);
            MemoryStream responseStream = new MemoryStream();

            HttpWebResponse response;
            SetConnectionTimeout(ConnectionRequest.Type);
            try
            {
                switch (ConnectionRequest.Type)
                {
                    case ConnectionType.Socket:
                        ConnectSocket();
                        SocketClient.Send(msgBytes);

                        byte[] messageReceived = new byte[1024];
                        int byteRecv = 0;
                        SocketClient.ReceiveTimeout = 1000;

                        do
                        {
                            byteRecv = SocketClient.Receive(messageReceived);
                            responseStream.Write(messageReceived, 0, byteRecv);
                        } while (SocketClient.Available > 0);

                        SocketClient.Shutdown(SocketShutdown.Both);
                        SocketClient.Close();

                        return new ConnectionResponse(responseStream.ToArray());

                    case ConnectionType.HttpSocket:
                    case ConnectionType.Http:
                    case ConnectionType.AWS:
                        SetHttpRequestMessageContentType(messageFormat, contentTypeStr);

                        if (!(HttpRequest.Method == "GET"))
                        {
                            HttpRequest.ContentLength = msgBytes.Length;

                            using (Stream requestBody = HttpRequest.GetRequestStream())
                            {
                                requestBody.Write(msgBytes, 0, msgBytes.Length);
                            }
                        }

                        response = (HttpWebResponse)HttpRequest.GetResponse();
                        return new ConnectionResponse(response);

                    default:
                        Logging.Error($"[SendMessage] :: Invalid ConnectionType, Request cannot be sent!");
                        break;
                }
            }
            catch (Exception ex)
            {
                FormMain.LogMessage(LogTypes.ERROR, $"[SendMessage] :: [{ConnectionRequest.Type}] :: [Error]\n>>>\n{ex}\n<<<");
            }

            return null;
        }

        /// <summary>
        /// Send a string message to the host asynchronously and receive a ConnectionResponse object
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="messageFormat">Format of the message</param>
        /// <returns>Task<ConnectionResponse></returns>
        public async Task<ConnectionResponse> SendMessageAsync(string message, MessageFormat messageFormat, string contentTypeStr)
        {
            Logging.Info($"[SendMessageAsync] :: Sending message via {ConnectionRequest.Type}");
            Logging.Info($"[SendMessageAsync] :: [Message To Be Sent]\n>>>\n{message}\n<<<");

            byte[] msgBytes = Encoding.ASCII.GetBytes(message);
            byte[] socketResp;

            HttpWebResponse response;

            SetConnectionTimeout(ConnectionRequest.Type);

            try
            {
                switch (ConnectionRequest.Type)
                {
                    case ConnectionType.Socket:
                        using (SocketClient sa = new SocketClient(ConnectionRequest.Host, (int)ConnectionRequest.Port))
                        {
                            sa.Connect();
                            socketResp = sa.SendReceive(msgBytes, Timeout);
                            sa.Disconnect();
                            sa.Dispose();
                        }
                        return new ConnectionResponse(socketResp);

                    case ConnectionType.HttpSocket:
                    case ConnectionType.Http:
                    case ConnectionType.AWS:
                        SetHttpRequestMessageContentType(messageFormat, contentTypeStr);

                        if (!(HttpRequest.Method == "GET"))
                        {
                            HttpRequest.ContentLength = msgBytes.Length;

                            using (Stream requestBody = HttpRequest.GetRequestStream())
                            {
                                await requestBody.WriteAsync(msgBytes, 0, msgBytes.Length);
                            }
                        }

                        response = (HttpWebResponse)await HttpRequest.GetResponseAsync();
                        return new ConnectionResponse(response);

                    default:
                        Logging.Error($"[SendMessageAsync] :: Invalid ConnectionType, Request cannot be sent!");
                        break;
                }

            }
            catch (Exception ex)
            {
                FormMain.LogMessage(LogTypes.ERROR, $"[SendMessageAsync] :: [{ConnectionRequest.Type}] :: [Error]\n>>>\n{ex}\n<<<");
            }
            return null;
        }

        /// <summary>
        /// Send a FileStream to the host and receive a ConnectionResponse object
        /// </summary>
        /// <param name="file">Filestream to send</param>
        /// <param name="fileFormat">Format of the file</param>
        /// <returns>ConnectionResponse</returns>
        public ConnectionResponse SendFile(FileStream file, FileFormat fileFormat, string contentTypeStr)
        {
            Logging.Info($"[SendFile] :: Sending file via {ConnectionRequest.Type}");

            var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);

            byte[] messageReceived = new byte[1024];
            MemoryStream responseStream = new MemoryStream();
            int byteRecv;

            byte[] HttpContent;
            HttpWebResponse response;

            SetConnectionTimeout(ConnectionRequest.Type);

            try
            {
                switch (ConnectionRequest.Type)
                {
                    case ConnectionType.Socket:
                        ConnectSocket();
                        SocketClient.Send(memoryStream.ToArray());

                        do
                        {
                            byteRecv = SocketClient.Receive(messageReceived);
                            responseStream.Write(messageReceived, 0, byteRecv);
                        } while (SocketClient.Available > 0);

                        SocketClient.Shutdown(SocketShutdown.Both);
                        SocketClient.Close();

                        return new ConnectionResponse(responseStream.ToArray());

                    case ConnectionType.HttpSocket:
                    case ConnectionType.Http:
                    case ConnectionType.AWS:
                        HttpContent = memoryStream.ToArray();
                        SetHttpRequestFileContentType(fileFormat, contentTypeStr);

                        if (!(HttpRequest.Method == "GET"))
                        {
                            HttpRequest.ContentLength = HttpContent.Length;

                            using (Stream requestBody = HttpRequest.GetRequestStream())
                            {
                                requestBody.Write(HttpContent, 0, HttpContent.Length);
                            }
                        }

                        response = (HttpWebResponse)HttpRequest.GetResponse();
                        return new ConnectionResponse(response);
                    default:
                        Logging.Error($"[SendFile] :: Invalid ConnectionType, Request cannot be sent!");
                        break;
                }
            }
            catch (Exception ex)
            {
                FormMain.LogMessage(LogTypes.ERROR, $"[SendFile] :: [{ConnectionRequest.Type}] :: [Error]\n>>>\n{ex}\n<<<");
            }
            return null;
        }

        /// <summary>
        /// Send a FileStream to the host asynchronously and receive a ConnectionResponse object
        /// </summary>
        /// <param name="file">Filestream to send</param>
        /// <param name="fileFormat">Format of the file</param>
        /// <returns></returns>
        public async Task<ConnectionResponse> SendFileAsync(FileStream file, FileFormat fileFormat, string contentTypeStr)
        {
            Logging.Info($"[SendFileAsync] :: Sending file via {ConnectionRequest.Type}");

            var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);

            byte[] socketResp;

            byte[] HttpContent;
            HttpWebResponse response;

            SetConnectionTimeout(ConnectionRequest.Type);

            try
            {
                switch (ConnectionRequest.Type)
                {
                    case ConnectionType.Socket:
                        using (SocketClient sa = new SocketClient(ConnectionRequest.Host, (int)ConnectionRequest.Port))
                        {
                            sa.Connect();
                            socketResp = sa.SendReceive(memoryStream.ToArray(), Timeout);
                            sa.Disconnect();
                            sa.Dispose();
                        }

                        return new ConnectionResponse(socketResp);

                    case ConnectionType.HttpSocket:
                    case ConnectionType.Http:
                    case ConnectionType.AWS:
                        HttpContent = memoryStream.ToArray();
                        SetHttpRequestFileContentType(fileFormat, contentTypeStr);

                        if (!(HttpRequest.Method == "GET"))
                        {
                            HttpRequest.ContentLength = HttpContent.Length;

                            using (Stream requestBody = await HttpRequest.GetRequestStreamAsync())
                            {
                                await requestBody.WriteAsync(HttpContent, 0, HttpContent.Length);
                            }
                        }

                        response = (HttpWebResponse)await HttpRequest.GetResponseAsync();
                        return new ConnectionResponse(response);

                    default:
                        Logging.Error($"[SendFileAsync] :: Invalid ConnectionType, Request cannot be sent!");
                        break;
                }
            }
            catch (Exception ex)
            {
                FormMain.LogMessage(LogTypes.ERROR, $"[SendFileAsync] :: [{ConnectionRequest.Type}] :: [Error]\n>>>\n{ex}\n<<<");
            }
            return null;
        }

        #region Helper Methods
        /// <summary>
        /// Aborts the HttpRequest
        /// </summary>
        private void CloseHttpRequest()
        {
            if (HttpRequest != null)
            {
                HttpRequest.Abort();
                HttpRequest = null;
            }
        }

        /// <summary>
        /// Set the Content-Type of the HttpRequest based on the MessageFormat
        /// </summary>
        /// <param name="msgFormat"></param>
        private void SetHttpRequestMessageContentType(MessageFormat msgFormat, string contentTypeStr)
        {
            if (HttpRequest == null) throw new Exception("HttpRequest does not exist");

            if (msgFormat == MessageFormat.Other)
            {
                HttpRequest.ContentType = contentTypeStr;
            }
            else
            {
                if (!MIME.MessageFormat.ContainsKey(msgFormat)) throw HostException.Error(ExceptionCode.INVALID_MESSAGE_FORMAT);
                HttpRequest.ContentType = MIME.MessageFormat[msgFormat];
            }
        }

        /// <summary>
        /// Set the Content-Type of the HttpRequest based on the FileFormat
        /// </summary>
        /// <param name="fileFormat"></param>
        private void SetHttpRequestFileContentType(FileFormat fileFormat, string contentTypeStr)
        {
            if (HttpRequest == null) throw new Exception("HttpRequest does not exist");

            if (fileFormat == FileFormat.Other)
            {
                HttpRequest.ContentType = contentTypeStr;
            }
            else
            {
                if (!MIME.FileFormat.ContainsKey(fileFormat)) throw HostException.Error(ExceptionCode.INVALID_FILE_FORMAT);
                HttpRequest.ContentType = MIME.FileFormat[fileFormat];
            }
        }

        /// <summary>
        /// Generates a WebRequest with data from HttpInfo
        /// </summary>
        /// <param name="connInfo"></param>
        /// <param name="httpInfo"></param>
        private void GenerateHttpRequest(ConnectionConstructor connInfo, HttpInfo? httpInfo)
        {
            string protocol = connInfo.Secure ? "https" : "http";
            string port = string.IsNullOrEmpty(connInfo.Port.ToString()) ? "" : $":{connInfo.Port}";
            string objectPath = httpInfo.HasValue ? httpInfo.Value.ObjectPath ?? "" : "";

            if (!string.IsNullOrEmpty(objectPath) && objectPath[0] != '\\' && objectPath[0] != '/')
            {
                objectPath = $"/{objectPath}";
            }

            string connectionString = $"{protocol}://" + connInfo.Host + port + objectPath;

            HttpRequest = WebRequest.CreateHttp(connectionString);
            Logging.Info($"[GenerateHttpRequest] :: HttpRequest initialized with {connectionString}");

            if (httpInfo.HasValue)
            {
                // Headers
                if (httpInfo.Value.HttpHeaders == null)
                {
                    HttpRequest.Headers = new WebHeaderCollection();
                }
                else
                {
                    // Custom headers are added here
                    HttpRequest.Headers = httpInfo.Value.HttpHeaders;
                }

                // Method
                if (string.IsNullOrEmpty(httpInfo.Value.HttpMethod))
                {
                    // Make default to GET method if no value
                    HttpRequest.Method = HttpMethod.Get.ToString();
                }
                else
                {
                    HttpRequest.Method = httpInfo.Value.HttpMethod;
                }

                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.Accept)) HttpRequest.Accept = httpInfo.Value.RestrictedHeaders.Accept;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.Connection)) HttpRequest.Connection = httpInfo.Value.RestrictedHeaders.Connection;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.ContentLength.ToString())) HttpRequest.ContentLength = httpInfo.Value.RestrictedHeaders.ContentLength;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.ContentType)) HttpRequest.ContentType = httpInfo.Value.RestrictedHeaders.ContentType;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.Date.ToString())) HttpRequest.Date = httpInfo.Value.RestrictedHeaders.Date;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.Expect)) HttpRequest.Expect = httpInfo.Value.RestrictedHeaders.Expect;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.Host)) HttpRequest.Host = httpInfo.Value.RestrictedHeaders.Host;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.IfModifiedSince.ToString())) HttpRequest.IfModifiedSince = httpInfo.Value.RestrictedHeaders.IfModifiedSince;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.Range)) HttpRequest.Headers["Range"] = httpInfo.Value.RestrictedHeaders.Range;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.Referer)) HttpRequest.Referer = httpInfo.Value.RestrictedHeaders.Referer;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.TransferEncoding)) HttpRequest.TransferEncoding = httpInfo.Value.RestrictedHeaders.TransferEncoding;
                if (!string.IsNullOrEmpty(httpInfo.Value.RestrictedHeaders.UserAgent)) HttpRequest.UserAgent = httpInfo.Value.RestrictedHeaders.UserAgent;
            }
        }

        /// <summary>
        /// Sets the request & response timeout based on connection type
        /// </summary>
        /// <param name="connType"></param>
        private void SetConnectionTimeout(ConnectionType connType)
        {
            switch (connType)
            {
                case ConnectionType.Socket:
                    SocketClient.ReceiveTimeout = Timeout;
                    SocketClient.SendTimeout = Timeout;
                    break;
                case ConnectionType.HttpSocket:
                case ConnectionType.Http:
                case ConnectionType.AWS:
                    HttpRequest.Timeout = Timeout;
                    break;
            }
        }
        #endregion Helper Methods

        /// <summary>
        /// Setup the connection, such as the HttpRequest or ClientSocket
        /// </summary>
        /// <param name="connInfo"></param>
        /// <param name="httpInfo"></param>
        /// <param name="awsInfo"></param>
        private void ConstructConnectionPreparer(ConnectionConstructor connInfo, HttpInfo? httpInfo, AWSInfo? awsInfo)
        {
            Logging.Info($"[ConstructConnectionPreparer] :: Constructing ConnectionPreparer for {connInfo.Type}");

            ConnectionRequest = connInfo;
            HttpInfo = httpInfo;
            AWSInfo = awsInfo;

            IsConnected = false;

            try
            {
                if (string.IsNullOrEmpty(connInfo.Host)) throw HostException.Error(ExceptionCode.HOST_EMPTY);

                switch (connInfo.Type)
                {
                    case ConnectionType.Socket:
                        if (!connInfo.Port.HasValue || string.IsNullOrEmpty(connInfo.Port.ToString())) throw HostException.Error(ExceptionCode.PORT_NOT_SET);

                        IPAddr = Dns.GetHostAddresses(connInfo.Host).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                        IPEndpoint = new IPEndPoint(IPAddr, (int)connInfo.Port);
                        ConnectSocket();
                        IsConnected = SocketConnected();
                        break;
                    case ConnectionType.HttpSocket:
                    case ConnectionType.Http:
                        GenerateHttpRequest(connInfo, httpInfo);
                        IsConnected = true;
                        break;
                    case ConnectionType.AWS: // Initiate AWS connection - JWTHelper
                        if (awsInfo.HasValue)
                        {
                            JWTHelper.doAPIAuth = true;
                            if (!JWTHelper.GetJwtToken(awsInfo.Value.MachId, connInfo.Host, awsInfo.Value.TokenURL, awsInfo.Value.Scope, out string errMsg, Timeout))
                            {
                                throw new Exception($"GetJwtToken Error :: {errMsg}");
                            }
                        }

                        GenerateHttpRequest(connInfo, httpInfo);

                        if (!string.IsNullOrEmpty(JWTHelper.jwtToken))
                        {
                            HttpRequest.Headers["Authorization"] = JWTHelper.jwtToken;
                            HttpRequest.Headers["cqmid"] = awsInfo.Value.MachId;
                        }

                        // Handling to resolve the underlying connection was closed
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        ServicePointManager.DefaultConnectionLimit = 50;
                        HttpRequest.ServicePoint.ConnectionLeaseTimeout = 0;

                        IsConnected = true;
                        break;
                    default:
                        throw HostException.Error(ExceptionCode.INVALID_CONNECTION_TYPE);
                }
            }
            catch (Exception ex)
            {
                Logging.Error("[ConstructConnectionPreparer] :: [Error] - " + ex);
            }

            if (IsConnected)
            {
                Logging.Info("[ConstructConnectionPreparer] :: Successfully Prepared Connection");
            }
        }

        /// <summary>
        /// Closes the connection to the host
        /// </summary>
        /// <returns>true/false depending on whether it closed correctly</returns>
        public bool Close()
        {
            Logging.Info($"[Close] :: [{ConnectionRequest.Type}] :: Closing connection to host");

            switch (ConnectionRequest.Type)
            {
                case ConnectionType.Socket:
                    if (SocketConnected())
                    {
                        SocketClient.Shutdown(SocketShutdown.Both);
                        SocketClient.Close();
                        SocketClient = null;
                    }
                    return true;
                case ConnectionType.HttpSocket:
                    CloseHttpRequest();
                    return true;
                case ConnectionType.Http:
                    CloseHttpRequest();
                    return true;
                case ConnectionType.AWS:
                    CloseHttpRequest();
                    return true;
                default:
                    Logging.Warn("[Close] :: Invalid Connection Type");
                    return false;
            }
        }

        #region Socket Functions
        private void ConnectSocket()
        {
            bool doConnectSocket = true;

            try
            {
                if (SocketClient != null)
                {
                    if (!SocketConnected())
                    {
                        SocketClient.Close();
                    }
                    else
                    {
                        doConnectSocket = false;
                    }
                }

                if (doConnectSocket)
                {
                    SocketClient = new Socket(IPAddr.AddressFamily, SocketType.Stream, PortType);
                    SocketClient.Connect(IPEndpoint);
                }
            }
            catch (Exception ex)
            {
                Logging.Error($"[ConnectSocket] :: [Error] - {ex.Message}");
                throw ex;
            }
        }

        private bool SocketConnected()
        {
            try
            {
                return !(SocketClient.Poll(1, SelectMode.SelectRead) && SocketClient.Available == 0) && SocketClient.Connected;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
        #endregion Socket Functions
    }
}
