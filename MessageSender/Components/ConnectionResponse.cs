using System;
using System.IO;
using System.Net;
using System.Text;

namespace MessageSender
{
    public class ConnectionResponse
    {
        public object Data { get; private set; }
        public DataType DataType { get;  private set; }

        readonly byte[] WebResponse;
        readonly string ContentType;

        public ExceptionCode Exception = ExceptionCode.OK;

        internal ConnectionResponse(object data)
        {
            Data = data;
            Type dataType = data.GetType();

            if (dataType == typeof(HttpWebResponse))
            {
                DataType = DataType.HttpWebResponse;

                HttpWebResponse response = (HttpWebResponse)Data;

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    Exception = ExceptionCode.BAD_REQUEST;
                }

                ContentType = response.ContentType.Split(';')[0];
                Logging.Debug("[ContentType] :: " + ContentType);

                using (Stream stream = response.GetResponseStream())
                using (MemoryStream mem = new MemoryStream())
                {
                    stream.CopyTo(mem);
                    WebResponse = mem.ToArray();

                    mem.Position = 0;

                    using (StreamReader reader = new StreamReader(mem))
                    {
                        Logging.Info("[Response] :: " + reader.ReadToEnd());
                    }
                }
            }
            else if (dataType == typeof(byte[]))
            {
                DataType = DataType.ByteArray;

                using (MemoryStream mem = new MemoryStream((byte[])data))
                using (StreamReader reader = new StreamReader(mem))
                {
                    Logging.Info("[Response] :: " + reader.ReadToEnd());
                }
            }
        }

        /// <summary>
        /// Return the response data
        /// </summary>
        /// <returns>Response data as an object</returns>
        public object GetData()
        {
            Logging.Debug("[GetData] :: Retrieve Response Data");
            return Data;
        }

        /// <summary>
        /// Return the response data as a string
        /// </summary>
        /// <returns>Encoded ASCII string of the response data</returns>
        public string CastToString()
        {
            Logging.Debug("[CastToString] :: Retrieve Response Data");

            switch (DataType)
            {
                case DataType.ByteArray:
                    return Encoding.ASCII.GetString((byte[])Data);
                case DataType.HttpWebResponse:
                    return Encoding.ASCII.GetString(WebResponse);
                default:
                    return Data.ToString();
            }
        }


        //public Dictionary<string, object> CastToDictionary()
        //{
        //    Logging.Info("[CastToDictionary] Response called");
        //    var ret = new Dictionary<string, object>();

        //    if (DataType == typeof(byte[]))
        //    {

        //    }
        //    else if (DataType == typeof(HttpWebResponse))
        //    {
        //        switch (ContentType)
        //        {
        //            case "application/json":
        //                string json = JsonConvert.DeserializeObject(CastToString()).ToString();
        //                Logging.Debug(json);
        //                ret = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        //                break;
        //            case "application/xml":
        //                XDocument xmlDoc = XDocument.Load(new MemoryStream(WebResponse));
        //                Logging.Debug(xmlDoc.ToString());

        //                if (xmlDoc.Descendants().FirstOrDefault().Name.LocalName == "string")
        //                {
        //                    string result = XElement.Parse(xmlDoc.ToString()).Value;
        //                    xmlDoc = XDocument.Parse(result);
        //                }

        //                List<string> varTypes = new List<string> { "string", "int" };

        //                foreach (XElement element in xmlDoc.Descendants().Where(p => p.HasElements == false)) // Todo - SubElements format to match json
        //                {
        //                    string name = element.Name.LocalName;
        //                    if (varTypes.Contains(name))
        //                    {
        //                        string parent = element.Parent.Name.LocalName;
        //                        if (ret.ContainsKey(parent))
        //                        {
        //                            //List<object> value = new List<object>();
        //                            //foreach (var prop in ret[parent].GetType().GetProperties())
        //                            //{
        //                            //    value = value.Add(prop.GetValue());
        //                            //}
        //                            ret[parent] = ret[parent] + "," + element.Value;
        //                        }
        //                        else
        //                        {
        //                            ret.Add(parent, element.Value);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        ret.Add(element.Name.LocalName, element.Value);
        //                    }
        //                }

        //                //using (XmlReader reader = XmlReader.Create(new MemoryStream(WebResponse)))
        //                //{
        //                //    reader.MoveToContent();

        //                //    var serializer = new XmlSerializer(typeof(JsonResponse));
        //                //    object xml = serializer.Deserialize(reader);

        //                //    ret = xml.GetType().GetProperties().ToDictionary
        //                //    (
        //                //        propInfo => propInfo.Name,
        //                //        propInfo => propInfo.GetValue(xml).ToString()
        //                //    );
        //                //}
        //                break;
        //            case "application/zip":
        //                break;
        //        }
        //    }
        //    return ret;

        //    //// Json Serialize & Deserialize
        //    //var json = JsonConvert.SerializeObject(Data);
        //    //return JsonConvert.DeserializeObject<Dictionary<string, TValue>>(json);

        //    //// LINQ expression
        //    //return Data.GetType().GetProperties().ToDictionary(prop => prop.Name, prop => (TValue)prop.GetValue(Data));

        //    //// Map
        //    //var dictionary = new Dictionary<string, TValue>();
        //    //foreach (PropertyInfo property in Data)
        //    //    AddPropertyToDictionary<T>(property, source, dictionary);
        //    //return dictionary;
        //}

        //public string CastToJson() // Working
        //{
        //    Logging.Info("[CastToJson] Response called");
        //    return JsonConvert.DeserializeObject(CastToString()).ToString();
        //}

        //public string CastToXML()
        //{
        //    Logging.Info("[CastToXML] Response called");
        //    return Data.ToString();

        //    // ToChange - deserialize
        //    try
        //    {
        //        using (var stringwriter = new System.IO.StringWriter())
        //        {
        //            var serializer = new XmlSerializer(this.GetType());
        //            serializer.Serialize(stringwriter, Data);
        //            return stringwriter.ToString();
        //        }
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        Logging.Warn("[CastToXML()] Error: Response cannot be serialized");
        //        return "";
        //    }
        //    catch (Exception ex)
        //    {
        //        Logging.Error("[CastToXML()] Error: " + ex);
        //        return "";
        //    }
        //}

        //public string CastToSoap()
        //{
        //    Logging.Info("[CastToSoap] Response called");
        //    return Data.ToString();

        //    // ToChange - deserialize
        //    try
        //    {
        //        using (var stringwriter = new System.IO.StringWriter())
        //        {
        //            // Serializes a class named Group as a SOAP message.
        //            XmlTypeMapping myTypeMapping =
        //                new SoapReflectionImporter().ImportTypeMapping(typeof(Group));

        //            var serializer = new XmlSerializer(myTypeMapping);
        //            serializer.Serialize(stringwriter, Data);
        //            return stringwriter.ToString();
        //        }
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        Logging.Warn("[CastToSOAP()] Error: Response cannot be serialized");
        //        return "";
        //    }
        //    catch (Exception ex)
        //    {
        //        Logging.Error("[CastToXML()] Error: " + ex);
        //        return "";
        //    }
        //}
    }
}
