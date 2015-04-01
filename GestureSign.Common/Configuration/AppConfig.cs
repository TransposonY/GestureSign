﻿using System;
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
        static System.Configuration.Configuration config;
        static System.Threading.Timer timer;
        public static event EventHandler ConfigChanged;
        static string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSign.exe");
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
        private static bool teaching = false;
        public static bool Teaching
        {
            get
            {
                return teaching;
            }
            set
            {
                teaching = value;
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
        //public static bool InterceptGlobalTouchInput
        //{
        //    get
        //    {
        //        return (bool)GetValue("InterceptGlobalTouchInput", true);
        //    }
        //    set
        //    {
        //        SetValue("InterceptGlobalTouchInput", value);
        //    }
        //}

        static AppConfig()
        {
            config = ConfigurationManager.OpenExeConfiguration(path);
            timer = new System.Threading.Timer(new TimerCallback(SaveFile), null, Timeout.Infinite, Timeout.Infinite);

        }

        public static void Reload()
        {
            try
            {

                int Count = 0;
                while (IsFileLocked(path + ".config") && Count != 10)
                {
                    Count++;
                    Thread.Sleep(20);
                }

                config = ConfigurationManager.OpenExeConfiguration(path);
                // ConfigurationManager.RefreshSection("appSettings");
                if (ConfigChanged != null)
                    ConfigChanged(new object(), EventArgs.Empty);
            }
            catch (Exception) { }
        }


        public static void Save()
        {
            timer.Change(400, Timeout.Infinite);
        }

        private static void SaveFile(object state)
        {
            try
            {
                int Count = 0;
                while (IsFileLocked(path + ".config") && Count != 10)
                {
                    Count++;
                    Thread.Sleep(20);
                }
                // Save the configuration file.    
                config.AppSettings.SectionInformation.ForceSave = true;
                config.Save(ConfigurationSaveMode.Minimal);
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
        private static bool IsFileLocked(string file)
        {
            try
            {
                using (File.Open(file, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException exception)
            {
                var errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 65535;
                return errorCode == 32 || errorCode == 33;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static object GetValue(string key, object defaultValue)
        {
            if (config.AppSettings.Settings[key] != null)
            {
                try
                {
                    string strReturn = config.AppSettings.Settings[key].Value;
                    if (defaultValue.GetType() == typeof(System.Drawing.Color)) return System.Drawing.ColorTranslator.FromHtml(strReturn);
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
        private static void SetValue(string key, System.Drawing.Color value)
        {
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = System.Drawing.ColorTranslator.ToHtml(value);
            }
            else
            {
                config.AppSettings.Settings.Add(key, System.Drawing.ColorTranslator.ToHtml(value));
            }
        }
    }
}