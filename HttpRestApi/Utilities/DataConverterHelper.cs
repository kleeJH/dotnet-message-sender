using System.Xml.Serialization;

namespace HttpRestApi.Utilities
{
    public static class DataConverterHelper
    {
        public static string? JsonToXml<T>(T json)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(string));

                // Serializes a class named Group as a XML message. 
                using var stringwriter = new StringWriter();
                serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stringwriter, json);
                return stringwriter.ToString();
            }
            catch (Exception ex)
            {
                Logging.Error("[Converter] :: [JsonToXml] :: [Error] - " + ex.ToString());
                return null;
            }
        }
    }
}
