using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MessageSender
{
    static class JWTHelper
    {
        public static string jwtToken { get; set; }

        public static bool doAPIAuth { get; set; } = false;

        public static string GenerateApiKey(string machId, out string errMsg)
        {
            errMsg = string.Empty;
            string hash;
            try
            {
                string message = machId + DateTime.Now.ToString("ddMMyyyy");
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(message));
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("x2"));
                    }

                    hash = builder.ToString();
                }
                return hash;
            }
            catch (Exception ex)
            {
                errMsg = "[GenerateApiKey] Exception: " + ex.Message;
                return string.Empty;
            }


        }

        internal static void GetTokenApiGateway(string URLpath, string MachId, string apiKey, string scope, out string errmsg, int timeout)
        {
            errmsg = "";
            jwtToken = null;
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(URLpath);
                request.Headers["username"] = MachId + "_" + apiKey;
                request.Headers["scope"] = scope;
                request.ContentType = "application/json";

                request.Timeout = timeout;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        jwtToken = reader.ReadToEnd();

                    }
                }
                jwtToken = jwtToken.Trim('"');

                ////if(jwtToken.Split('.').Length != 3)                     //not a valid JWTToken, possibly error message 
                ////{
                ////    errmsg = jwtToken;
                ////}
            }
            catch (Exception ex)
            {
                errmsg = ex.Message;
            }
        }

        internal static bool GetJwtToken(string MachID, string HostName, string JwtTokenUrl, string ScopeRequest, out string errMsg, int timeout)
        {
            string tokenUrl = string.Empty;
            string apiKey = string.Empty;
            errMsg = "";


            if (doAPIAuth)
            {
                if (string.IsNullOrEmpty(MachID))
                {
                    Logging.Error($"[GetJwtToken] Exception: {ExceptionCode.MACH_ID_EMPTY}");
                    return false;
                }

                apiKey = GenerateApiKey(MachID, out errMsg);
                if (string.IsNullOrEmpty(apiKey))
                {
                    Logging.Error("[GetJwtToken] GenerateApiKey error: " + errMsg);
                    return false;
                }

                if (String.IsNullOrEmpty(JwtTokenUrl))
                {
                    Logging.Error($"[GetJwtToken] Exception: {ExceptionCode.TOKEN_URL_EMPTY}");
                    return false;
                }
                tokenUrl = "https://" + HostName + "/" + JwtTokenUrl;

                GetTokenApiGateway(tokenUrl, MachID, apiKey, ScopeRequest, out errMsg, timeout);
                if (string.IsNullOrEmpty(jwtToken))
                {
                    errMsg += errMsg + "|[tokenUrl:" + tokenUrl + "]|[apiKey:" + apiKey + "]";
                    Logging.Error("[GetJwtToken] GetTokenApiGateway error: " + errMsg);
                    return false;
                }

                Logging.Info("[GetJwtToken] Token successfully retreived");
                return true;
            }
            else
            {
                Logging.Warn("[GetJwtToken] doAPIAuth = false");
                return true;
            }

        }
    }
}

