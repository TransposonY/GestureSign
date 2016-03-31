using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.IO;
using GestureSign.Common.InterProcessCommunication;

namespace GestureSign.Common.Configuration
{
    public class AppConfig
    {
        static System.Configuration.Configuration _config;
        static readonly System.Threading.Timer Timer;
        public static event EventHandler ConfigChanged;

        private static readonly string Path;

        private static readonly ExeConfigurationFileMap ExeMap;

        public static System.Drawing.Color VisualFeedbackColor
        {
            get
            {
                return (System.Drawing.Color)GetValue("VisualFeedbackColor", System.Drawing.Color.DeepSkyBlue);
            }
            set
            {
                SetValue("VisualFeedbackColor", value);
            }
        }

        public static int VisualFeedbackWidth
        {
            get
            {
                return (int)GetValue("VisualFeedbackWidth", 20);
            }
            set
            {
                SetValue("VisualFeedbackWidth", value);
            }
        }
        public static int MinimumPointDistance
        {
            get
            {
                return (int)GetValue("MinimumPointDistance", 20);
            }
            set
            {
                SetValue("MinimumPointDistance", value);
            }
        }

        public static double Opacity
        {
            get
            {
                return (double)GetValue("Opacity", 0.35);
            }
            set
            {
                SetValue("Opacity", value);
            }
        }

        public static bool IsOrderByLocation
        {
            get
            {
                return (bool)GetValue("IsOrderByLocation", true);
            }
            set
            {
                SetValue("IsOrderByLocation", value);
            }
        }

        public static bool UiAccess { get; set; }
        public static bool ShowTrayIcon
        {
            get
            {
                return (bool)GetValue("ShowTrayIcon", true);
            }
            set
            {
                SetValue("ShowTrayIcon", value);
            }
        }
        public static bool ShowBalloonTip
        {
            get
            {
                return (bool)GetValue("ShowBalloonTip", false);
            }
            set
            {
                SetValue("ShowBalloonTip", value);
            }
        }
        public static string CultureName
        {
            get
            {
                return (string)GetValue("CultureName", "");
            }
            set
            {
                SetValue("CultureName", value);
            }
        }

        public static bool SendErrorReport
        {
            get
            {
                return (bool)GetValue("SendErrorReport", true);
            }
            set
            {
                SetValue("SendErrorReport", value);
            }
        }

        public static DateTime LastErrorTime
        {
            get
            {
                return GetValue("LastErrorTime", DateTime.MinValue);
            }
            set
            {
                SetValue("LastErrorTime", value);
            }
        }

        public static int GestureTimeout
        {
            get
            {
                return (int)GetValue("Timeout", 800);
            }
            set
            {
                SetValue("Timeout", value);
            }
        }

        static AppConfig()
        {
#if uiAccess
            UiAccess = true;
#endif
            Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"GestureSign\GestureSign.config");
            var configFolder = System.IO.Path.GetDirectoryName(Path);

            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            ExeMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = Path,
                RoamingUserConfigFilename = Path,
                LocalUserConfigFilename = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"GestureSign\GestureSign.config")
            };

            _config = ConfigurationManager.OpenMappedExeConfiguration(ExeMap, ConfigurationUserLevel.None);
            Timer = new Timer(SaveFile, null, Timeout.Infinite, Timeout.Infinite);

        }

        public static void Reload()
        {
            try
            {
                FileManager.WaitFile(Path);

                _config = ConfigurationManager.OpenMappedExeConfiguration(ExeMap, ConfigurationUserLevel.None);
                // ConfigurationManager.RefreshSection("appSettings");
                if (ConfigChanged != null)
                    ConfigChanged(new object(), EventArgs.Empty);
            }
            catch (Exception e) { Logging.LogException(e); }
        }


        public static void Save()
        {
            Timer.Change(400, Timeout.Infinite);
        }

        private static void SaveFile(object state)
        {
            try
            {
                FileManager.WaitFile(Path);
                // Save the configuration file.    
                _config.AppSettings.SectionInformation.ForceSave = true;
                _config.Save(ConfigurationSaveMode.Modified);
            }
            catch (ConfigurationErrorsException)
            {
                Reload();
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }
            // Force a reload of the changed section.    
            ConfigurationManager.RefreshSection("appSettings");

            NamedPipe.SendMessageAsync("LoadConfiguration", "GestureSignDaemon");
        }

        private static object GetValue(string key, object defaultValue)
        {
            var setting = _config.AppSettings.Settings[key];
            if (setting != null)
            {
                try
                {
                    string strReturn = setting.Value;
                    if (defaultValue.GetType() == typeof(System.Drawing.Color)) return System.Drawing.ColorTranslator.FromHtml(strReturn);
                    else if (defaultValue is int) return int.Parse(strReturn);
                    else if (defaultValue is double) return double.Parse(strReturn);
                    else if (defaultValue is bool) return bool.Parse(strReturn);
                    //return string
                    else return strReturn;
                }
                catch
                {
                    SetValue(key, defaultValue);
                    return defaultValue;
                }
            }
            else return defaultValue;
        }

        private static DateTime GetValue(string key, DateTime defaultValue)
        {
            var setting = _config.AppSettings.Settings[key];
            if (setting != null)
            {
                try
                {
                    return DateTime.Parse(setting.Value);
                }
                catch
                {
                    SetValue(key, defaultValue);
                    return defaultValue;
                }
            }
            else return defaultValue;
        }

        private static void SetValue(string key, object value)
        {
            if (_config.AppSettings.Settings[key] != null)
            {
                _config.AppSettings.Settings[key].Value = value.ToString();
            }
            else
            {
                _config.AppSettings.Settings.Add(key, value.ToString());
            }
        }
        private static void SetValue(string key, System.Drawing.Color value)
        {
            if (_config.AppSettings.Settings[key] != null)
            {
                _config.AppSettings.Settings[key].Value = System.Drawing.ColorTranslator.ToHtml(value);
            }
            else
            {
                _config.AppSettings.Settings.Add(key, System.Drawing.ColorTranslator.ToHtml(value));
            }
        }

        private static void SetValue(string key, DateTime value)
        {
            if (_config.AppSettings.Settings[key] != null)
            {
                _config.AppSettings.Settings[key].Value = value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                _config.AppSettings.Settings.Add(key, value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
