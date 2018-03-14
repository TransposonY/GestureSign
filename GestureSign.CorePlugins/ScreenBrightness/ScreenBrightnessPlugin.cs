///<summary>
///
///Tiny tool for quickly adjusting screen brightness of laptops and tablets
///Tested on win7 (x86)
///needs .net 4 (but should compile on lower versions, too...probably You have to rebuild the form - dunno)
///Does not work on normal pcs...as far as I know...
///free to use and whatever...but not for sale...^^
///code for wmi queries stolen from Samuel Lai http://edgylogic.com/projects/display-brightness-vista-gadget/ :)
///
///</summary>

using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using Microsoft.Win32;

namespace GestureSign.CorePlugins.ScreenBrightness
{
    class ScreenBrightnessPlugin : IPlugin
    {
        #region Private Variables

        private static int? _currentBrightness;
        private static int _brightnessSetting;
        private ScreenBrightnessUI _GUI = null;
        private BrightnessSettings _Settings = null;

        private enum Method
        {
            BrightnessUp = 0,
            BrightnessDown = 1
        }
        #endregion

        #region PInvoke Declarations

        [DllImport("powrprof.dll")]
        public static extern uint PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);

        #endregion

        #region Public Properties
        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.ScreenBrightness.Name"); }
        }

        public string Description
        {
            get { return GetDescription(_Settings); }
        }

        public object GUI
        {
            get
            {
                if (_GUI == null)
                    _GUI = CreateGUI();

                return _GUI;
            }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public ScreenBrightnessUI TypedGUI
        {
            get { return (ScreenBrightnessUI)GUI; }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.ScreenBrightness.Category"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon => IconSource.Brightness;

        #endregion

        #region Public Methods


        public void Initialize()
        {

        }
        public bool Gestured(PointInfo ActionPoint)
        {
            return AdjustBrightness(_Settings);
        }

        public bool Deserialize(string SerializedData)
        {
            return PluginHelper.DeserializeSettings(SerializedData, out _Settings);
        }

        public string Serialize()
        {
            if (_GUI != null)
                _Settings = _GUI.Settings;

            if (_Settings == null)
                _Settings = new BrightnessSettings();

            return PluginHelper.SerializeSettings(_Settings);
        }

        #endregion

        #region Private Methods

        private ScreenBrightnessUI CreateGUI()
        {
            ScreenBrightnessUI newGUI = new ScreenBrightnessUI();

            newGUI.Loaded += (o, e) =>
            {
                TypedGUI.Settings = _Settings;
                TypedGUI.HostControl = HostControl;
            };

            return newGUI;
        }

        private string GetDescription(BrightnessSettings Settings)
        {
            if (Settings == null)
                return LocalizationProvider.Instance.GetTextValue("CorePlugins.ScreenBrightness.Name");

            // Create string to store final output description
            string strOutput = "";

            // Build output string
            switch (Settings.Method)
            {
                case 0:
                    strOutput = LocalizationProvider.Instance.GetTextValue("CorePlugins.ScreenBrightness.IncreaseBrightness") + Settings.Percent + " %";
                    break;
                case 1:
                    strOutput = LocalizationProvider.Instance.GetTextValue("CorePlugins.ScreenBrightness.DecreaseBrightness") + Settings.Percent + " %";
                    break;
            }

            return strOutput;
        }

        private bool AdjustBrightness(BrightnessSettings Settings)
        {
            if (Settings == null)
                return false;
            try
            {
                int currentBrightness = GetBrightness();
                byte[] level = GetBrightnessLevels();
                byte maxLevel = level.Max();
                byte minLevel = level.Min();
                int levelChange = Settings.Percent * maxLevel / 100;
                int targetValue;
                byte targetLevel = 0;

                switch ((Method)_Settings.Method)
                {
                    case Method.BrightnessUp:
                        targetValue = currentBrightness + levelChange > maxLevel ? maxLevel : currentBrightness + levelChange;
                        targetLevel = Array.Find(level, l => l >= targetValue);
                        SetBrightness(targetLevel);
                        break;
                    case Method.BrightnessDown:
                        targetValue = currentBrightness - levelChange < minLevel ? minLevel : currentBrightness - levelChange;
                        targetLevel = Array.Find(level, l => l >= targetValue);
                        SetBrightness(targetLevel);
                        break;
                }
                _currentBrightness = targetLevel;
                return true;
            }
            catch
            {
                //MessageBox.Show("Could not change volume settings.", "Volume Change Invalid", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static int GetBrightness()
        {
            int brightness = GetSettingBrightness();
            if (brightness >= 0 && brightness != _brightnessSetting)
            {
                _brightnessSetting = brightness;
                return brightness;
            }

            if (_currentBrightness != null)
                return _currentBrightness.Value;

            if (brightness < 0)
                brightness = GetActualBrightness();

            _currentBrightness = brightness;
            return brightness;
        }

        /// <summary>
        /// Get brightness from registry, which can not be change by WmiSetBrightness
        /// </summary>
        /// <returns></returns>
        private static int GetSettingBrightness()
        {
            try
            {
                Guid guid = GetActiveSchemeGuid();
                var powerStatus = SystemInformation.PowerStatus.PowerLineStatus;
                if (guid != Guid.Empty)
                {
                    using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes\" + guid +
                        @"\7516b95f-f776-4464-8c53-06167f40cc99\aded5e82-b909-4619-9949-f5d71dac0bcb"))
                    {
                        if (rk != null)
                        {
                            var result = rk.GetValue(powerStatus == PowerLineStatus.Online ? "ACSettingIndex" : "DCSettingIndex");
                            if (result != null)
                                return (int)result;
                        }
                    }
                }
                return -1;
            }
            catch
            {
                return -1;
            }
        }

        //get the actual percentage of brightness
        static int GetActualBrightness()
        {
            //define scope (namespace)
            ManagementScope s = new ManagementScope("root\\WMI");

            //define query
            SelectQuery q = new SelectQuery("WmiMonitorBrightness");

            //output current brightness
            byte curBrightness = 0;
            using (ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q))
            {
                using (ManagementObjectCollection moc = mos.Get())
                {
                    foreach (ManagementObject o in moc)
                    {
                        curBrightness = (byte)o["CurrentBrightness"];
                        break; //only work on the first object
                    }
                }
            }

            return curBrightness;
        }

        //array of valid brightness values in percent
        static byte[] GetBrightnessLevels()
        {
            //define scope (namespace)
            ManagementScope s = new ManagementScope("root\\WMI");

            //define query
            SelectQuery q = new SelectQuery("WmiMonitorBrightness");

            //store result
            byte[] brightnessLevels = new byte[0];
            //output current brightness
            using (ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q))
            {
                try
                {
                    using (ManagementObjectCollection moc = mos.Get())
                    {
                        foreach (ManagementObject o in moc)
                        {
                            brightnessLevels = (byte[])o.GetPropertyValue("Level");
                            break; //only work on the first object
                        }
                    }
                }
                catch (Exception)
                {
                    // MessageBox.Show("Sorry, Your System does not support this brightness control...");
                }
            }
            return brightnessLevels;
        }

        //static void SetBrightness(byte targetBrightness)
        //{
        //    int timeout = 1;// UInt32.MaxValue
        //    //define scope (namespace)
        //    ManagementScope s = new ManagementScope("root\\WMI");

        //    //define query
        //    SelectQuery q = new SelectQuery("WmiMonitorBrightnessMethods");

        //    //output current brightness
        //    using (ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q))
        //    {
        //        using (ManagementObjectCollection moc = mos.Get())
        //        {
        //            foreach (ManagementObject o in moc)
        //            {
        //                o.InvokeMethod("WmiSetBrightness", new Object[] { timeout, targetBrightness }); //note the reversed order - won't work otherwise!
        //                break; //only work on the first object
        //            }
        //        }
        //    }
        //}

        private void SetBrightness(byte brightness)
        {
            using (var brightnessMethods = new ManagementClass("root/wmi", "WmiMonitorBrightnessMethods", null))
            using (var inParams = brightnessMethods.GetMethodParameters("WmiSetBrightness"))
            {
                foreach (var o in brightnessMethods.GetInstances())
                {
                    var mo = (ManagementObject)o;
                    inParams["Brightness"] = brightness; // set brightness to brightness %
                    inParams["Timeout"] = 1;
                    mo.InvokeMethod("WmiSetBrightness", inParams, null);
                    break;
                }
            }
        }

        private static Guid GetActiveSchemeGuid()
        {
            Guid activeScheme = Guid.Empty;
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                if (PowerGetActiveScheme((IntPtr)null, out ptr) == 0)
                {
                    activeScheme = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
                }
                return activeScheme;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
