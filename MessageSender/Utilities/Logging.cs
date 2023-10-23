using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MessageSender
{
    public enum LogTypes
    {
        DEBUG,
        INFO,
        WARN,
        ERROR
    }

    public static class Logging
    {
        public static readonly string DEFAULT_DIRECTORY = Application.StartupPath + @"\Logs\";
        public static string LogFilePath { get; set; } = DEFAULT_DIRECTORY;
        public static string LogFileName { get; set; }

        internal static void Info(string message)
        {
            Write(message, LogTypes.INFO);
        }

        internal static void Warn(string message)
        {
            Write(message, LogTypes.WARN);
        }

        internal static void Error(string message)
        {
            Write(message, LogTypes.ERROR);
        }

        internal static void Debug(string message)
        {
            Write(message, LogTypes.DEBUG);
        }

        public static string Write(string message, LogTypes logType)
        {
            string logMsg = "";

            switch (logType)
            {
                case LogTypes.INFO:
                    logMsg = "[Info]  ";
                    break;
                case LogTypes.WARN:
                    logMsg = "[Warn]  ";
                    break;
                case LogTypes.ERROR:
                    logMsg = "[Error] ";
                    break;
                case LogTypes.DEBUG:
                    logMsg = "[Debug] ";
                    break;
            }

            logMsg += DateTime.Now.ToString(":: [dd/MM/yyyy HH:mm:ss] :: ") + message;

            using (Mutex mutex = new Mutex(false, "MS.LogMessage.Mutex"))
            {
                mutex.WaitOne();

                try
                {
                    if (LogFilePath == null)
                    {
                        LogFilePath = DEFAULT_DIRECTORY;
                    }
                    
                    if (!Directory.Exists(LogFilePath))
                    {
                        Directory.CreateDirectory(LogFilePath);
                    }
                }
                catch
                {
                    LogFilePath = DEFAULT_DIRECTORY;

                    if (!Directory.Exists(LogFilePath))
                    {
                        Directory.CreateDirectory(LogFilePath);
                    }
                }

                if (string.IsNullOrEmpty(LogFileName))
                {
                    LogFileName = "MS_" + DateTime.Now.ToString("dd-MM-yyyy HH.mm") + ".log";
                }

                string path = Path.Combine(LogFilePath, LogFileName);

                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    sw.WriteLine(logMsg);
                }

                mutex.ReleaseMutex();
                return logMsg;
            }
        }
    }
}
