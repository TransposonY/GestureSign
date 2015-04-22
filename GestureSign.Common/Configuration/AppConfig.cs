using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using System.Threading;
using System.IO;

namespace GestureSign.Common.Configuration
{
    public class AppConfig
    {
        static System.Configuration.Configuration _config;
        static readonly System.Threading.Timer Timer;
        public static event EventHandler ConfigChanged;

        private static readonly string Path;

        private static readonly FileSystemWatcher Fsw;

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
        public static int ReversalAngleThreshold
        {
            get
            {
                return (int)GetValue("ReversalAngleThreshold", 160);
            }
            set
            {
                SetValue("ReversalAngleThreshold", value);
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
        private static double? xRatio;
        public static double XRatio
        {
            get
            {
                return xRatio ?? (xRatio = (double)GetValue("XRatio", 0.0)).Value;
            }
            set
            {
                xRatio = value;
                SetValue("XRatio", value);
            }
        }
        private static double? yRatio;
        public static double YRatio
        {
            get
            {
                return yRatio ?? (yRatio = (double)GetValue("YRatio", 0.0)).Value;
            }
            set
            {
                yRatio = value;
                SetValue("YRatio", value);
            }
        }

        public static bool Teaching { get; set; }

        public static string DeviceName
        {
            get
            {
                return (string)GetValue("DeviceName", string.Empty);
            }
            set
            {
                SetValue("DeviceName", value);
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
        public static bool InterceptTouchInput
        {
            get
            {
                return (bool)GetValue("InterceptTouchInput", true);
            }
            set
            {
                SetValue("InterceptTouchInput", value);
            }
        }

        public static bool CompatibilityMode
        {
            get
            {
                return (bool)GetValue("CompatibilityMode", false);
            }
            set
            {
                SetValue("CompatibilityMode", value);
            }
        }

        static AppConfig()
        {
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
            Teaching = false;
            _config = ConfigurationManager.OpenMappedExeConfiguration(ExeMap, ConfigurationUserLevel.None);
            Timer = new Timer(SaveFile, null, Timeout.Infinite, Timeout.Infinite);

            Fsw = new FileSystemWatcher(configFolder)
            {
                Filter = "*.config",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                IncludeSubdirectories = false
            };
            Fsw.Created += fsw_Changed;
            Fsw.Changed += fsw_Changed;
        }
        static void fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name.Equals("gesturesign.config", StringComparison.CurrentCultureIgnoreCase))
                Reload();
        }
        public static void ToggleWatcher()
        { Fsw.EnableRaisingEvents = !Fsw.EnableRaisingEvents; }

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
            catch (Exception) { }
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
            catch (Exception)
            {
            }
            // Force a reload of the changed section.    
            ConfigurationManager.RefreshSection("appSettings");
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
    }
}
