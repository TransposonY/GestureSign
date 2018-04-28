using System;
using System.IO;
using GestureSign.Common.Configuration;

namespace GestureSign.Common.Log
{
    public class Logging
    {
        private static string _logFilePath;

        private class StreamWriterWithTimestamp : StreamWriter
        {
            public StreamWriterWithTimestamp(Stream stream) : base(stream)
            {
            }

            private string GetTimestamp()
            {
                return "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
            }

            private string GetVersion()
            {
                return "[" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + "] ";
            }

            public override void WriteLine(string value)
            {
                base.WriteLine(GetTimestamp() + GetVersion() + value);
            }

            public override void Write(string value)
            {
                base.Write(GetTimestamp() + GetVersion() + value);
            }
        }

        public static string LogFilePath => _logFilePath;

        public static bool OpenLogFile()
        {
            bool result;
            try
            {
                _logFilePath = Path.Combine(AppConfig.ApplicationDataPath, "GestureSign.log");
                CheckLogSize(_logFilePath);
                var sw = new StreamWriterWithTimestamp(new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
                Console.SetOut(sw);
                Console.SetError(sw);
                result = true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                result = false;
            }
            return result;
        }

        public static void LogException(Exception e)
        {
            if (!(e is ObjectDisposedException))
            {
                Console.WriteLine(e);
                Console.WriteLine();
                if (e.InnerException != null)
                    LogException(e.InnerException);
            }
        }

        public static void LogMessage(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine();
        }

        private static void CheckLogSize(string logPath)
        {
            if (File.Exists(logPath))
            {
                if (new FileInfo(logPath).Length > 10240)
                    File.Delete(logPath);
            }
        }
    }
}
