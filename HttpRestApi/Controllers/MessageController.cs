using Newtonsoft.Json;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using HttpRestApi.Utilities;
using HttpRestApi.Models;

namespace HttpRestApi.Controllers
{
    /// <summary>
    /// This controller is used to receive a message for different content types via REST Api. It will then return
    /// some sort of message back to the client.
    /// 
    /// Route:
    /// - messsage/plaintext :: Receives plain text and returns the plain text
    /// - messsage/json :: Receives ExampleRequestModel object and returns ResponseModel
    /// - messsage/xml :: Not implemented. Requires an InputFormatter. Attempt to call this route will result in UnsupportMediaType error (415). JsonToXml converter is available.
    /// - messsage/soap :: Not implemented. Requires an InputFormatter. Attempt to call this route will result in UnsupportMediaType error (415).
    /// 
    /// Note:
    /// - This controller is able to log the parameters (query) that is present in the URI
    /// </summary>
    [ApiController]
    [Route("message")]
    public class MessageController : ControllerBase
    {
        [HttpPost]
        [Route("plaintext")]
        [Consumes("text/plain")]
        public IActionResult ProcessPlainTextMessage([FromBody] string message)
        {

            return ProcessMessage(RequestFormat.Plaintext, message);

        }

        [HttpPost]
        [Route("json")]
        [Consumes("application/json")]
        public IActionResult ProcessJsonMessage([FromBody] ExampleRequestModel model)
        {
            return ProcessMessage(RequestFormat.JSON, model);
        }

        [HttpPost]
        [Route("xml")]
        [Consumes("application/xml")]
        public IActionResult ProcessXmlMessage([FromBody] string message) // XML not applicable, need to add InputFormatter
        {

            return ProcessMessage(RequestFormat.XML, message);
        }

        [HttpPost]
        [Route("soap")]
        [Consumes("application/soap+xml")]
        public IActionResult ProcessSoapMessage([FromBody] string message) // SOAP not applicable, need to add InputFormatter
        {

            return ProcessMessage(RequestFormat.SOAP, message);
        }


        [NonAction]
        public IActionResult ProcessMessage(RequestFormat format, object requestData)
        {
            // Log Request
            Logging.Info($"[{Request.Method}] :: [{format}] :: Processing Message");

            // Headers
            //Logging.Info("[Header] User-Agent :: " + Request.Headers.UserAgent);
            //try { Logging.Info("[Header] Keep-Alive :: " + Request.Headers.KeepAlive); } catch { }

            // Custom Headers
            //try { Logging.Info("[Header] UserAgent :: " + Request.Headers.TryGetValue("UserAgent", out var userAgent)); } catch { }

            // Log Request Parameters
            //Logging.Debug("[Request] Query String :: " + Request.QueryString.ToString());

            NameValueCollection queryParameters = new();
            var queries = Request.Query;
            string logQueries = $"[{Request.Method}] :: [{format}] :: Query Parameters <Key, Value> :: ";
            foreach (var query in queries)
            {
                queryParameters.Add(query.Key, query.Value);
                logQueries += $"<{query.Key}, {query.Value}>";
            }

            if (queries.Count > 0)
            {
                Logging.Info(logQueries);
            }

            try
            {
                string responseMsg;
                // Simulate data processing & JsonResponse based on ContentType
                switch (format)
                {
                    case RequestFormat.Plaintext:
                        responseMsg = (string)requestData;
                        break;
                    case RequestFormat.JSON:
                        ResponseModel JsonResponse = new()
                        {
                            Status = true,
                            Data = (ExampleRequestModel)requestData, 
                            Message = "Received",
                        };
                        System.Diagnostics.Debug.WriteLine(JsonResponse.Data.ToString());
                        responseMsg = JsonConvert.SerializeObject(JsonResponse);
                        break;
                    case RequestFormat.XML:
                        //responseMsg = DataConverterHelper.JsonToXml((ExampleRequestModel)requestData)!;
                        return BadRequest("Unable to process request, XML's InputFormatter not found!");
                    case RequestFormat.SOAP:
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
                            var serializer = new XmlSerializer(typeof(LoginRequest.Envelope));
                            var settings = new XmlWriterSettings
                            {
                                Encoding = Encoding.UTF8,
                                Indent = true,
                                OmitXmlDeclaration = false,
                            };
                            var builder = new StringBuilder();
                            using (var writer = XmlWriter.Create(builder, settings))
                            {
                                serializer.Serialize(writer, env, env.xmlns);
                            }
                            responseMsg = builder.ToString();
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("[Response] SOAP Error :: " + ex.ToString());
                        }
                        return BadRequest("Unable to process request, SOAP's InputFormatter not found!");
                    default:
                        return BadRequest("Incorrect Request Format!");
                }

                Logging.Info("[Response] Response :: " + responseMsg);
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
}
