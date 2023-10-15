using Newtonsoft.Json;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;

namespace HttpRestApi.Controllers
{
    /// <summary>
    /// This controller is used to receive a message via REST Api. It will then return
    /// the same message as a response to the client.
    /// 
    /// Route:
    /// - messsage :: Able to receive a string value from the body
    /// 
    /// Note:
    /// - This controller is able to log the parameters (query) that is present in the URI
    /// </summary>
    [ApiController]
    [Route("message")]
    public class MessageController : ControllerBase
    {
        [HttpPost]
        public IActionResult ProcessMessage([FromBody] string value)
        {
            try
            {
                // Log Request
                Logging.Info(Request.ToString()!);


                // Headers
                Logging.Info("[Header] User-Agent :: " + Request.Headers.UserAgent);
                try { Logging.Info("[Header] Keep-Alive :: " + Request.Headers.KeepAlive); } catch { }

                // Custom Headers
                //try { Logging.Info("[Header] UserAgent :: " + Request.Headers.TryGetValue("UserAgent", out var userAgent)); } catch { }


                // Request Body
                string requestBody = value;
                Logging.Info("[Request] Body :: " + requestBody);


                // If request has query parameters
                //Logging.Debug("[Request] Query String :: " + Request.QueryString.ToString());

                NameValueCollection queryParameters = new();
                var queries = Request.Query;
                string logQueries = "[Request] Query Parameters <Key, Value> :: ";
                foreach (var query in queries)
                {
                    queryParameters.Add(query.Key, query.Value);
                    logQueries += $"<{query.Key}, {query.Value}>";
                }

                if (queries.Count > 0)
                {
                    Logging.Info(logQueries);
                }


                // Simulate data processing & response based on ContentType
                string responseMsg = "";
                JsonResponse jsonResp = new()
                {
                    Content = requestBody,
                    Response = "Received",
                    SubClass = new SubClass()
                };
                jsonResp.SubClass = new SubClass
                {
                    S1 = new string[] { "Test 1", "Test 2" }
                };

                var serializer = new XmlSerializer(typeof(string));


                switch (Request.ContentType)
                {
                    case "text/plain":
                        responseMsg = requestBody;
                        break;
                    case "application/json":
                        jsonResp.ContentType = "application/json";
                        responseMsg = JsonConvert.SerializeObject(jsonResp);
                        break;
                    case "application/xml":
                        jsonResp.ContentType = "application/xml";
                        try
                        {
                            // Serializes a class named Group as a XML message. 
                            using var stringwriter = new StringWriter();
                            serializer = new XmlSerializer(typeof(JsonResponse));
                            serializer.Serialize(stringwriter, jsonResp);
                            responseMsg = stringwriter.ToString();
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("[Response] XML Error :: " + ex.ToString());
                        }
                        break;
                    case "application/soap+xml":
                        try
                        {
                            var env = new LoginRequest.Envelope
                            {
                                Header = new LoginRequest.Header(),
                                Body = new LoginRequest.Body()
                                {
                                    LoginRequest = new LoginRequest
                                    {
                                        //   Id = "0",
                                        //   Root = 1,
                                        DeviceId = "***",
                                        Firm = "***",
                                        Login = "***",
                                        Password = "***"
                                    },
                                },
                            };
                            serializer = new XmlSerializer(typeof(LoginRequest.Envelope));
                            var settings = new XmlWriterSettings
                            {
                                Encoding = Encoding.UTF8,
                                Indent = true,
                                OmitXmlDeclaration = false,
                            };
                            var builder = new StringBuilder();
                            using (var writer = XmlWriter.Create(builder, settings))
                            {
                                serializer.Serialize(writer, env, env.Xmlns);
                            }
                            responseMsg = builder.ToString();
                            Logging.Debug("[Response] SOAP Response :: " + responseMsg);
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("[Response] SOAP Error :: " + ex.ToString());
                        }
                        break;
                }

                // Return response
                return Ok(responseMsg);
            }
            catch (Exception ex)
            {
                Logging.Error("[ProcessMessage] Error :: " + ex.ToString());
            }

            Logging.Empty();

            return BadRequest();
        }
    }

    // JSON, XML
    [Serializable]
    public struct JsonResponse
    {
        public string ContentType { get; set; }
        public string Content { get; set; }
        public string Response { get; set; }
        public SubClass SubClass { get; set; }
    }

    [Serializable]
    public struct SubClass
    {
        public string[] S1 { get; set; }
        public int[] S2 { get; set; }
    }

    // SOAP, ref: https://stackoverflow.com/questions/53533520/serialize-object-c-sharp-to-soap-request
    [XmlType(Namespace = LoginRequest.m, IncludeInSchema = true)]
    public struct LoginRequest
    {
        //private const string i = "http://www.w3.org/2001/XMLSchema-instance";
        private const string m = "http://www.w3.org/2001/XMLSchema";
        //private const string c = "http://schemas.xmlsoap.org/soap/encoding/";
        private const string v = "http://schemas.xmlsoap.org/soap/envelope/";
        //private const string t = "http://tasks.ws.com/";

        // [XmlAttribute(AttributeName = "id")]
        // public string Id { get; set; }
        // [XmlAttribute(AttributeName = "root", Namespace = c)]
        // public int Root { get; set; }

        [XmlElement(ElementName = "firm")]
        public string Firm { get; set; }

        [XmlElement(ElementName = "login")]
        public string Login { get; set; }

        [XmlElement(ElementName = "password")]
        public string Password { get; set; }

        [XmlElement(ElementName = "device_id")]
        public string DeviceId { get; set; }

        [XmlRoot(Namespace = v)]
        public struct Envelope
        {
            public Header Header { get; set; }
            public Body Body { get; set; }

            static Envelope()
            {
                staticxmlns = new XmlSerializerNamespaces();
                //staticxmlns.Add("i", i);
                staticxmlns.Add("m", m);
                //staticxmlns.Add("c", c);
                staticxmlns.Add("soap", v);
            }
            private static readonly XmlSerializerNamespaces staticxmlns;
            [XmlNamespaceDeclarations]
            public readonly XmlSerializerNamespaces Xmlns { get { return staticxmlns; } set { } }
        }

        [XmlType(Namespace = v)]
        public class Header { }

        [XmlType(Namespace = v)]
        public class Body
        {
            public LoginRequest LoginRequest { get; set; }
        }
    }
}
