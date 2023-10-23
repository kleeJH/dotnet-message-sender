using System;

namespace MessageSender
{
    public abstract class ConnectionConstructor
    {
        internal ConnectionType Type;
        internal bool Secure;
        internal string Host;
        internal UInt16? Port;

        /// <summary>
        /// Instantiates a ConnectionPreparer object
        /// </summary>
        /// <returns>ConnectionPreparer</returns>
        public ConnectionPreparer Create(FormMain formMain)
        {

            return new ConnectionPreparer(formMain, this);
        }

        /// <summary>
        /// Instantiates ConnectionPreparer with overridden HTTP values
        /// </summary>
        /// <param name="httpinfo"></param>
        /// <returns>ConnectionPreparer</returns>
        public ConnectionPreparer Create(FormMain formMain, HttpInfo httpinfo)
        {
            return new ConnectionPreparer(formMain, this, httpinfo);
        }

        /// <summary>
        /// Instantiates ConnectionPreparer with additional AWS values
        /// </summary>
        /// <param name="awsInfo"></param>
        /// <returns>ConnectionPreparer</returns>
        public ConnectionPreparer Create(FormMain formMain, AWSInfo awsInfo)
        {
            return new ConnectionPreparer(formMain, this, awsInfo);
        }

        /// <summary>
        /// Instantiates ConnectionPreparer with overriden HTTP values and additional AWS values
        /// </summary>
        /// <param name="httpinfo"></param>
        /// <param name="awsInfo"></param>
        /// <returns>ConnectionPreparer</returns>
        public ConnectionPreparer Create(FormMain formMain, HttpInfo httpinfo, AWSInfo awsInfo)
        {
            return new ConnectionPreparer(formMain, this, httpinfo, awsInfo);
        }

        /// <summary>
        /// Constructor method to set the connection variables
        /// </summary>
        /// <param name="type"></param>
        /// <param name="secure"></param>
        /// <param name="host"></param>
        /// <param name="machId"></param>
        /// <param name="port"></param>
        internal void Construct(ConnectionType type, bool secure, string host, UInt16? port)
        {
            Type = type;
            Secure = secure;
            Host = host;
            Port = port;
        }
    }

    public class SocketRequest : ConnectionConstructor
    {
        public SocketRequest(bool secure, string host, UInt16? port)
        {
            Construct(ConnectionType.Socket, secure, host, port);
            Logging.Info("[SocketRequest] :: Object Instantiated");
        }
    }

    public class HttpSocketRequest : ConnectionConstructor
    {
        public HttpSocketRequest(bool secure, string host, UInt16? port)
        {
            Construct(ConnectionType.HttpSocket, secure, host, port);
            Logging.Info("[HttpSocketRequest] :: Object Instantiated");
        }
    }

    public class HttpRequest : ConnectionConstructor
    {
        public HttpRequest(bool secure, string host, UInt16? port)
        {
            Construct(ConnectionType.Http, secure, host, port);
            Logging.Info("[HttpRequest] :: Object Instantiated");
        }
    }

    public class AWSRequest : ConnectionConstructor
    {
        public AWSRequest(bool secure, string host, UInt16? port)
        {
            Construct(ConnectionType.AWS, secure, host, port);
            Logging.Info("[AWSRequest] :: Object Instantiated");
        }
    }
}
