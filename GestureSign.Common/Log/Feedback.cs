using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows;
using GestureSign.Common.Configuration;
using Microsoft.Win32;
using SharpRaven;
using SharpRaven.Data;

namespace GestureSign.Common.Log
{
    public class Feedback
    {
        private const string Dsn = "https://a828c0c755fc493fa93c0f2ac7963e6d:4e74093b0f6a4a438a95b3bb85273e69@sentry.io/141461";

        public static string Send(string report, string message)
        {
            string sendError = null;
            var ravenClient = new RavenClient(Dsn)
            {
                ErrorOnCapture = e =>
                {
                    Logging.LogException(e);
                    sendError = e.Message;
                },
                Compression = true
            };

            const int chunkSize = 4096;
            if (!String.IsNullOrWhiteSpace(report))
            {
                foreach (string s in Split(report, chunkSize))
                {
                    if (s != string.Empty)
                        ravenClient.Capture(new SentryEvent(s));
                }
            }

            if (!string.IsNullOrWhiteSpace(message))
                ravenClient.Capture(new SentryEvent(message) { Level = ErrorLevel.Info });

            return sendError;
        }

        public static string OutputLog()
        {
            StringBuilder result = new StringBuilder(2048);

            var rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (rk != null)
                result.AppendLine(rk.GetValue("ProductName") + " " + rk.GetValue("BuildLabEx") + " " + rk.GetValue("UBR"));

            string version = FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location).FileVersion +
                           (Environment.Is64BitProcess ? " X64" : " x86") +
                        (AppConfig.UiAccess ? " UIAccess" : "");

            using (RegistryKey layers = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
            {
                string daemonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");
                var daemonRecord = layers?.GetValue(daemonPath) as string;
                if (daemonRecord != null && daemonRecord.Contains("WIN8RTM")) version += " CompatibilityMode";
            }
            result.AppendLine(version);

            string directoryPath = Path.GetDirectoryName(new Uri(Application.ResourceAssembly.CodeBase).LocalPath);
            if (directoryPath != null)
            {
                result.AppendLine(directoryPath);
            }
            result.AppendLine();

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_computersystem");
                foreach (ManagementObject mo in searcher.Get())
                {
                    try
                    {
                        result.AppendLine(mo["Manufacturer"].ToString().Trim());
                        result.AppendLine(mo["Model"].ToString().Trim());
                        break;
                    }
                    catch { }
                }
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct");
                foreach (ManagementObject mo in searcher.Get())
                {
                    try
                    {
                        result.AppendLine(mo["Version"].ToString().Trim());
                    }
                    catch { }
                }
            }
            catch (Exception e)
            {
                result.AppendLine(e.ToString());
            }

            result.AppendLine();
            result.AppendLine();

            if (File.Exists(Logging.LogFilePath))
            {
                try
                {
                    FileStream fs = new FileStream(Logging.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using (StreamReader streamReader = new StreamReader(fs))
                        result.Append(streamReader.ReadToEnd());
                }
                catch (Exception e)
                {
                    result.AppendLine(e.ToString());
                }
            }
            result.AppendLine();
            result.AppendLine();

            EventLog logs = new EventLog { Log = "Application" };

            foreach (EventLogEntry entry in logs.Entries)
            {
                if (entry.EntryType == EventLogEntryType.Error && ".NET Runtime".Equals(entry.Source) &&
                    entry.Message.IndexOf("GestureSign", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.AppendLine(entry.TimeWritten.ToString(CultureInfo.InvariantCulture));
                    result.AppendLine(entry.Message.Replace("\n", "\r\n"));
                }
            }

            return result.ToString();
        }

        private static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize + 1)
                .Select(i => str.Substring(i * chunkSize, (i * chunkSize + chunkSize <= str.Length) ? chunkSize : str.Length - i * chunkSize));
        }
    }
}

