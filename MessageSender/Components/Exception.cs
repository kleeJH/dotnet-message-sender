using System;

namespace MessageSender
{
    public static class HostException
    {
        public static Exception Error(ExceptionCode exCode, string msgOverride = "")
        {
            string msg = exCode.ToString();

            if (string.IsNullOrEmpty(msgOverride))
            {
                switch (exCode)
                {
                    case ExceptionCode.HOST_EMPTY:
                        msg += ":: Hostname is required to connect";
                        break;
                    case ExceptionCode.PORT_NOT_SET:
                        msg += ":: Port value is required to connect";
                        break;
                    case ExceptionCode.FILENAME_EMPTY:
                        msg += ":: Filename is required";
                        break;
                    case ExceptionCode.MACH_ID_EMPTY:
                    case ExceptionCode.TOKEN_URL_EMPTY:
                    case ExceptionCode.INVALID_CONNECTION_TYPE:
                    case ExceptionCode.INVALID_MESSAGE_FORMAT:
                    case ExceptionCode.INVALID_FILE_FORMAT:
                        break;
                }
            }
            else
            {
                msg += $":: {msgOverride}";
            }

            return new Exception(msg);
        }
    }
}
