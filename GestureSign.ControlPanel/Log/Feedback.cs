using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows;
using GestureSign.Common;
using GestureSign.Common.Configuration;
using GestureSign.Common.Log;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace GestureSign.ControlPanel.Log
{
    public class Feedback
    {
        private const string Dsn = "https://a828c0c755fc493fa93c0f2ac7963e6d:4e74093b0f6a4a438a95b3bb85273e69@sentry.io/141461";
        private static readonly Uri SentryDsn = new Uri(Dsn);
        private static readonly Uri DeliveryEndpoint = new UriBuilder(SentryDsn)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Path = "/api" + SentryDsn.AbsolutePath + "/store/"
        }.Uri;

        public static string Send(string report)
        {
            return Send(report, DeliveryEndpoint, TimeSpan.FromSeconds(30));
        }

        internal static string Send(string report, Uri endpoint, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(report))
                return null;

            try
            {
                using (var cancellation = new CancellationTokenSource(timeout))
                using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
                    { Timeout = Timeout.InfiniteTimeSpan })
                {
                    string[] credentials = SentryDsn.UserInfo.Split(':');
                    client.DefaultRequestHeaders.Add("X-Sentry-Auth",
                        "Sentry sentry_version=7, sentry_key=" + credentials[0] +
                        ", sentry_secret=" + credentials[1]);

                    const int chunkSize = 4096;
                    foreach (string chunk in Split(report, chunkSize))
                    {
                        if (chunk.Length == 0)
                            continue;

                        string payload = JsonConvert.SerializeObject(new
                        {
                            event_id = Guid.NewGuid().ToString("N"),
                            timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                            platform = "csharp",
                            level = "info",
                            message = new { formatted = chunk }
                        });
                        using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                        {
                            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                            // A drained SDK queue is not an acknowledgement from the server.
                            using (var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead,
                                cancellation.Token))
                            {
                                response.EnsureSuccessStatusCode();
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception exception)
            {
                Logging.LogException(exception);
                return exception.Message;
            }
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
                string daemonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.DaemonFileName);
                var daemonRecord = layers?.GetValue(daemonPath) as string;
                if (daemonRecord != null && daemonRecord.Contains("WIN8RTM")) version += " CompatibilityMode";
            }
            result.AppendLine(version);

            string directoryPath = Path.GetDirectoryName(Application.ResourceAssembly.Location);
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

