using System.Xml.Serialization;

namespace HttpRestApi.Models
{
    // Reference: https://stackoverflow.com/questions/53533520/serialize-object-c-sharp-to-soap-request

    [XmlType(Namespace = LoginRequest.t, IncludeInSchema = true)]
    public class LoginRequest
    {
        private const string i = "http://www.w3.org/2001/XMLSchema-instance";
        private const string d = "http://www.w3.org/2001/XMLSchema";
        private const string c = "http://schemas.xmlsoap.org/soap/encoding/";
        private const string v = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string t = "http://tasks.ws.com/";

        //  [XmlAttribute(AttributeName = "id")]
        // public string Id { get; set; }
        // [XmlAttribute(AttributeName = "root", Namespace = c)]
        // public int Root { get; set; }

        [XmlElement(ElementName = "firm", Namespace = v)]
        public required string Firm { get; set; }

        [XmlElement(ElementName = "login")]
        public required string Login { get; set; }

        [XmlElement(ElementName = "password")]
        public required string Password { get; set; }

        [XmlElement(ElementName = "device_id")]
        public required string DeviceId { get; set; }

        [XmlRoot(Namespace = v)]
        public class Envelope
        {
            public required Header Header { get; set; }
            public required Body Body { get; set; }

            static Envelope()
            {
                staticxmlns = new XmlSerializerNamespaces();
                staticxmlns.Add("i", i);
                staticxmlns.Add("d", d);
                staticxmlns.Add("c", c);
                staticxmlns.Add("soapenv", v);
            }
            private static XmlSerializerNamespaces staticxmlns;
            [XmlNamespaceDeclarations]
            public XmlSerializerNamespaces xmlns { get { return staticxmlns; } set { } }
        }

        [XmlType(Namespace = v)]
        public class Header { }

        [XmlType(Namespace = v)]
        public class Body
        {
            [XmlElement(ElementName = "login", Namespace = t)]
            public required LoginRequest LoginRequest { get; set; }
        }
    }
}

//[XmlType(Namespace = LoginRequest.m, IncludeInSchema = true)]
//public struct LoginRequest
//{
//    private const string m = "http://www.w3.org/2001/XMLSchema";
//    private const string v = "http://schemas.xmlsoap.org/soap/envelope/";

//    [XmlElement(ElementName = "firm")]
//    public string Firm { get; set; }

//    [XmlElement(ElementName = "login")]
//    public string Login { get; set; }

//    [XmlElement(ElementName = "password")]
//    public string Password { get; set; }

//    [XmlElement(ElementName = "device_id")]
//    public string DeviceId { get; set; }

//    [XmlRoot(Namespace = v)]
//    public struct Envelope
//    {
//        public Header Header { get; set; }
//        public Body Body { get; set; }

//        static Envelope()
//        {
//            staticxmlns = new XmlSerializerNamespaces();
//            staticxmlns.Add("m", m);
//            staticxmlns.Add("soap", v);
//        }
//        private static readonly XmlSerializerNamespaces staticxmlns;
//        [XmlNamespaceDeclarations]
//        public readonly XmlSerializerNamespaces Xmlns { get { return staticxmlns; } set { } }
//    }

//    [XmlType(Namespace = v)]
//    public class Header { }

//    [XmlType(Namespace = v)]
//    public class Body
//    {
//        public LoginRequest LoginRequest { get; set; }
//    }
//}