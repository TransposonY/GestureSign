using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GestureSign.Common;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using Microsoft.Win32;

namespace GestureSign.ControlPanel.Common
{
    class ErrorReport
    {
        private const string Address = "gesturesignfeedback00@yahoo.com";

        public static string SendMail(string subject, string content)
        {
            MailMessage mail = new MailMessage(Address, Address)
            {
                Subject = subject,
                Body = content,
                IsBodyHtml = false
            };

            SmtpClient client = new SmtpClient
            {
                // UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(Address, Int32.MaxValue.ToString()),
                // Port = 25,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Host = "smtp.mail.yahoo.com",
                EnableSsl = true,
                Timeout = 36000,
            };
            // client.Port = 587

            try
            {
                client.Send(mail);
                //client.SendAsync(mail, userState);

                return null;
            }
            catch (SmtpException ex)
            {
                Logging.LogException(ex);
                return ex.Message;
            }
        }

        public static void OutputLog(ref StringBuilder result)
        {
            if (result == null) result = new StringBuilder(1000);

            var rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (rk != null)
                result.AppendLine(rk.GetValue("ProductName") + " " + rk.GetValue("BuildLabEx"));

            string version = LocalizationProvider.Instance.GetTextValue("About.Version") +
                           FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location).FileVersion +
                           (Environment.Is64BitProcess ? " X64" : " x86") +
                        (AppConfig.UiAccess ? " UIAccess" : "");

            using (RegistryKey layers = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
            {
                string daemonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");
                var daemonRecord = layers?.GetValue(daemonPath) as string;
                if (daemonRecord != null && daemonRecord.Contains("WIN8RTM")) version += " CompatibilityMode";
            }

            result.AppendLine(version);

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

            result.AppendLine();
            result.AppendLine();

            if (File.Exists(Logging.LogFilePath))
            {
                using (FileStream fs = new FileStream(Logging.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader streamReader = new StreamReader(fs))
                        result.Append(streamReader.ReadToEnd());
                }
            }

            EventLog logs = new EventLog { Log = "Application" };

            foreach (EventLogEntry entry in logs.Entries)
            {
                if (entry.EntryType == EventLogEntryType.Error && ".NET Runtime".Equals(entry.Source))
                {
                    result.AppendLine(entry.TimeWritten.ToString(CultureInfo.InvariantCulture));
                    result.AppendLine(entry.Message.Replace("\n", "\r\n"));
                }
            }
        }
    }
}

