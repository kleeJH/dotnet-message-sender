using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text.Encodings.Web;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace MessageSender
{
    #region Enums
    public enum SendType
    {
        File,
        Message
    }
    public enum ConnectionType
    {
        Socket,
        HttpSocket,
        Http,
        AWS
    }

    public enum MessageFormat
    {
        Plaintext,
        XML,
        JSON,
        SOAP,
        Other
    }

    public enum FileFormat
    {
        ZIP,
        SOAP,
        XML,
        JSON,
        Other
    }

    public enum ExceptionCode
    {
        OK,
        HOST_EMPTY,
        PORT_NOT_SET,
        MACH_ID_EMPTY,
        TOKEN_URL_EMPTY,
        FILENAME_EMPTY,
        INVALID_CONNECTION_TYPE,
        INVALID_MESSAGE_FORMAT,
        INVALID_FILE_FORMAT,
        BAD_REQUEST
    }

    public enum DataType
    {
        ByteArray,
        HttpWebResponse,
    }
    #endregion Enums

    #region Structs
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AWSInfo
    {
        public string Scope;
        public string MachId;
        public string TokenURL;

        /// <summary>
        /// Construct a new AWSInfo instance with existing values
        /// </summary>
        /// <param name="_awsInfo"></param>
        public AWSInfo(AWSInfo _awsInfo)
        {
            Scope = _awsInfo.Scope;
            MachId = _awsInfo.MachId;
            TokenURL = _awsInfo.TokenURL;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HttpInfo
    {
        public string ObjectPath;
        public WebHeaderCollection HttpHeaders;
        public string HttpMethod;
        public RestrictedHeaderCollection RestrictedHeaders;

        /// <summary>
        /// Construct a new HttpInfo instance with existing values
        /// </summary>
        /// <param name="_httpInfo"></param>
        public HttpInfo(HttpInfo _httpInfo)
        {
            ObjectPath = _httpInfo.ObjectPath;
            HttpHeaders = _httpInfo.HttpHeaders;
            HttpMethod = _httpInfo.HttpMethod;
            RestrictedHeaders = _httpInfo.RestrictedHeaders;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RestrictedHeaderCollection
    {
        public string Accept;
        public string Connection;
        public long ContentLength;
        public string ContentType;
        public DateTime Date;
        public string Expect;
        public string Host;
        public DateTime IfModifiedSince;
        public string Range;
        public string Referer;
        public string TransferEncoding;
        public string UserAgent;
    }
    #endregion Structs

    #region Constants
    public static class MIME
    {
        public static readonly Dictionary<MessageFormat, string> MessageFormat = new Dictionary<MessageFormat, string>()
        {
            { MessageSender.MessageFormat.Plaintext, "text/plain" },
            { MessageSender.MessageFormat.XML, "application/xml" },
            { MessageSender.MessageFormat.JSON, "application/json" },
            { MessageSender.MessageFormat.SOAP, "application/soap+xml" }
        };

        public static readonly Dictionary<FileFormat, string> FileFormat = new Dictionary<FileFormat, string>()
        {
            { MessageSender.FileFormat.SOAP, "application/soap+xml" },
            { MessageSender.FileFormat.XML, "application/xml" },
            { MessageSender.FileFormat.JSON, "application/json" },
            { MessageSender.FileFormat.ZIP, "application/zip" }
        };

        // Reference: https://stackoverflow.com/questions/23714383/what-are-all-the-possible-values-for-http-content-type-header
        public static readonly List<string> ContentTypes = new List<string>()
        {
             // Application
             "application/java-archive",
             "application/EDI-X12",
             "application/EDIFACT",
             "application/javascript",
             "application/octet-stream",
             "application/ogg",
             "application/pdf",
             "application/xhtml+xml",
             "application/x-shockwave-flash",
             "application/json",
             "application/ld+json",
             "application/xml",
             "application/soap+xml",
             "application/zip",
             "application/x-www-form-urlencoded",

             // Audio
             "audio/mpeg",
             "audio/x-ms-wma",
             "audio/vnd.rn-realaudio",
             "audio/x-wav",

             // Image
             "image/gif",
             "image/jpeg",
             "image/png",
             "image/tiff",
             "image/vnd.microsoft.icon",
             "image/x-icon",
             "image/vnd.djvu",
             "image/svg+xml",

             // Multipart
             "multipart/mixed",
             "multipart/alternative",
             "multipart/related", // Used by MHTML(HTML mail).) 
             "multipart/form-data",

             // Text
             "text/css",
             "text/csv",
             "text/html",
             "text/javascript", // Obsolete
             "text/plain",
             "text/xml",

             // Video
             "video/mpeg",
             "video/mp4",
             "video/quicktime",
             "video/x-ms-wmv",
             "video/x-msvideo",
             "video/x-flv",
             "video/webm",

             // Vendor
             "application/vnd.android.package-archive",
             "application/vnd.oasis.opendocument.text",
             "application/vnd.oasis.opendocument.spreadsheet",
             "application/vnd.oasis.opendocument.presentation",
             "application/vnd.oasis.opendocument.graphics",
             "application/vnd.ms-excel",
             "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
             "application/vnd.ms-powerpoint",
             "application/vnd.openxmlformats-officedocument.presentationml.presentation",
             "application/msword",
             "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
             "application/vnd.mozilla.xul+xml"
};
    }
    #endregion Constants
}
