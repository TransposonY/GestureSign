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

            private string GetNameAndVersion()
            {
                var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                return $"[{assemblyName.Name} v{assemblyName.Version}] ";
            }

            public override void WriteLine(string value)
            {
                base.WriteLine(GetTimestamp() + GetNameAndVersion() + value);
            }

            public override void Write(string value)
            {
                base.Write(GetTimestamp() + GetNameAndVersion() + value);
            }
        }

        public static string LogFilePath => _logFilePath;
        public static event EventHandler<Exception> LoggedExceptionOccurred;

        public static bool OpenLogFile()
        {
            bool result;
            try
            {
                _logFilePath = Path.Combine(AppConfig.LocalApplicationDataPath, "GestureSign.log");
                CheckLogSize(_logFilePath);
                var sw = new StreamWriterWithTimestamp(new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
                Console.SetOut(sw);
                Console.SetError(sw);
                result = true;
            }
            catch (Exception e)
            {
                LogAndNotice(e);
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

        public static void LogAndNotice(Exception e)
        {
            LogException(e);
            LoggedExceptionOccurred?.Invoke(null, e);
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
                if (new FileInfo(logPath).Length > 102400)
                    File.Delete(logPath);
            }
        }
    }
}
