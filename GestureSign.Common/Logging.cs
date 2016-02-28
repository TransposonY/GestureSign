using System;
using System.IO;

namespace GestureSign.Common
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

            public override void WriteLine(string value)
            {
                base.WriteLine(GetTimestamp() + value);
            }

            public override void Write(string value)
            {
                base.Write(GetTimestamp() + value);
            }
        }

        public static string LogFilePath => _logFilePath;

        public static bool OpenLogFile()
        {
            bool result;
            try
            {
                _logFilePath = Path.Combine(Path.GetTempPath(), "GestureSign.log");
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
            }
        }
    }
}
