using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace GestureSign.Configuration
{
    public class AppConfig
    {
        static System.Configuration.Configuration config;
        public static System.Windows.Media.Color VisualFeedbackColor
        {
            get
            {
                return (System.Windows.Media.Color)GetValue("VisualFeedbackColor", System.Windows.Media.Colors.DeepSkyBlue);
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
                return (int)GetValue("MinimumPointDistance", 12);
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
        public static double XRatio
        {
            get
            {
                return (double)GetValue("XRatio", 0.0);
            }
            set
            {
                SetValue("XRatio", value);
            }
        }
        public static double YRatio
        {
            get
            {
                return (double)GetValue("YRatio", 0.0);
            }
            set
            {
                SetValue("YRatio", value);
            }
        }
        public static bool Teaching
        {
            get
            {
                return (bool)GetValue("Teaching", false);
            }
            set
            {
                SetValue("Teaching", value);
            }
        }
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

        static AppConfig()
        {
            config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        public static void Save()
        {
            // Save the configuration file.    
            config.AppSettings.SectionInformation.ForceSave = true;
            config.Save(ConfigurationSaveMode.Minimal);
            // Force a reload of the changed section.    
            ConfigurationManager.RefreshSection("appSettings");
        }

        private static object GetValue(string key, object defaultValue)
        {
            if (config.AppSettings.Settings[key] != null)
            {
                try
                {
                    string strReturn = config.AppSettings.Settings[key].Value;
                    if (defaultValue.GetType() == typeof(System.Windows.Media.Color)) return System.Windows.Media.ColorConverter.ConvertFromString(strReturn);
                    else if (defaultValue.GetType() == typeof(int)) return int.Parse(strReturn);
                    else if (defaultValue.GetType() == typeof(double)) return double.Parse(strReturn);
                    else if (defaultValue.GetType() == typeof(bool)) return bool.Parse(strReturn);
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
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = value.ToString();
            }
            else
            {
                config.AppSettings.Settings.Add(key, value.ToString());
            }
        }
    }
}
