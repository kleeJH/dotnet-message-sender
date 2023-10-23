namespace HttpRestApi
{
    public static class Logging
    {
        public static string LogPath { get; set; } = Directory.GetCurrentDirectory() + @"\Logs\";
        public static string LogFileName { get; set; } = "API.log";

        public static void Info(string message)
        {
            Write(message, LogTypes.INFO);
        }

        public static void Warn(string message)
        {
            Write(message, LogTypes.WARN);
        }

        public static void Error(string message)
        {
            Write(message, LogTypes.ERROR);
        }

        public static void Debug(string message)
        {
            Write(message, LogTypes.DEBUG);
        }

        public static void Empty()
        {
            Write("\n", null);
        }

        private static void Write(string message, LogTypes? logType)
        {
            try
            {
                if (!Directory.Exists(LogPath))
                {
                    Directory.CreateDirectory(LogPath);
                }
            }
            catch
            {
                LogPath = Directory.GetCurrentDirectory() + @"\Logs\";
                if (!Directory.Exists(LogPath))
                {
                    Directory.CreateDirectory(LogPath);
                }
            }

            string filePath = LogPath + LogFileName;
            string logMsg = "";

            if (!(logType == null))
            {
                switch (logType)
                {
                    case LogTypes.INFO:
                        logMsg = "- " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff : ") + message;
                        break;
                    case LogTypes.WARN:
                        logMsg = "W " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff : ") + message;
                        break;
                    case LogTypes.ERROR:
                        logMsg = "E " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff : ") + message;
                        break;
                    case LogTypes.DEBUG:
                        logMsg = "D " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff : ") + message;
                        break;
                }
            }

            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine(logMsg);
            }

        }
    }

    public enum LogTypes
    {
        DEBUG,
        INFO,
        WARN,
        ERROR
    }
}

