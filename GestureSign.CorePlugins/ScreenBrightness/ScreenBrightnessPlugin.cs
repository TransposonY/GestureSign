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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.IO;
using System.Runtime.Serialization.Json;

using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins.ScreenBrightness
{
    class ScreenBrightnessPlugin : IPlugin
    {
        private ScreenBrightnessUI _GUI = null;
        private BrightnessSettings _Settings = null;

        private enum Method
        {
            BrightnessUp = 0,
            BrightnessDown = 1
        }

        #region Public Properties
        public string Name
        {
            get { return "调整屏幕亮度"; }
        }

        public string Description
        {
            get { return GetDescription(_Settings); }
        }

        public UserControl GUI
        {
            get
            {
                if (_GUI == null)
                    _GUI = CreateGUI();

                return _GUI;
            }
        }

        public ScreenBrightnessUI TypedGUI
        {
            get { return (ScreenBrightnessUI)GUI; }
        }

        public string Category
        {
            get { return "系统"; }
        }

        public bool IsAction
        {
            get { return true; }
        }

        #endregion

        #region Public Methods


        public void Initialize()
        {

        }
        public bool Gestured(Common.Plugins.PointInfo ActionPoint)
        {
            return AdjustBrightness(_Settings);
        }

        public void Deserialize(string SerializedData)
        {
            // Clear existing settings if nothing was passed in
            if (String.IsNullOrEmpty(SerializedData))
            {
                _Settings = new BrightnessSettings();
                return;
            }

            // Create memory stream from serialized data string
            MemoryStream memStream = new MemoryStream(Encoding.Default.GetBytes(SerializedData));

            // Create json serializer to deserialize json file
            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(BrightnessSettings));

            // Deserialize json file into actions list
            _Settings = jSerial.ReadObject(memStream) as BrightnessSettings;

            if (_Settings == null)
                _Settings = new BrightnessSettings();
        }

        public string Serialize()
        {
            _Settings = _GUI.Settings;

            if (_Settings == null)
                _Settings = new BrightnessSettings();

            // Create json serializer to serialize json file
            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(BrightnessSettings));

            // Open json file
            MemoryStream mStream = new MemoryStream();
            StreamWriter sWrite = new StreamWriter(mStream);

            // Serialize actions into json file
            jSerial.WriteObject(mStream, _Settings);

            return Encoding.Default.GetString(mStream.ToArray());
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
                return "调整屏幕亮度";

            // Create string to store final output description
            string strOutput = "";

            // Build output string
            switch (Settings.Method)
            {
                case 0:
                    strOutput = "增大亮度 " + Settings.Percent.ToString() + "%";
                    break;
                case 1:
                    strOutput = "减小亮度 " + Settings.Percent.ToString() + "%";
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
                switch ((Method)_Settings.Method)
                {
                    case Method.BrightnessUp:
                        SetBrightness((byte)((currentBrightness + Settings.Percent) > 100 ? 100 : currentBrightness + Settings.Percent));
                        break;
                    case Method.BrightnessDown:
                        SetBrightness((byte)((currentBrightness - Settings.Percent) < 0 ? 0 : currentBrightness - Settings.Percent));
                        break;
                }

                return true;
            }
            catch
            {
                //MessageBox.Show("Could not change volume settings.", "Volume Change Invalid", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }



        //get the actual percentage of brightness
        static int GetBrightness()
        {
            //define scope (namespace)
            System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

            //define query
            System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightness");

            //output current brightness
            System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);

            System.Management.ManagementObjectCollection moc = mos.Get();

            //store result
            byte curBrightness = 0;
            foreach (System.Management.ManagementObject o in moc)
            {
                curBrightness = (byte)o.GetPropertyValue("CurrentBrightness");
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();

            return (int)curBrightness;
        }

        //array of valid brightness values in percent
        static byte[] GetBrightnessLevels()
        {
            //define scope (namespace)
            System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

            //define query
            System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightness");

            //output current brightness
            System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);
            byte[] BrightnessLevels = new byte[0];

            try
            {
                System.Management.ManagementObjectCollection moc = mos.Get();

                //store result


                foreach (System.Management.ManagementObject o in moc)
                {
                    BrightnessLevels = (byte[])o.GetPropertyValue("Level");
                    break; //only work on the first object
                }

                moc.Dispose();
                mos.Dispose();

            }
            catch (Exception)
            {
                // MessageBox.Show("Sorry, Your System does not support this brightness control...");

            }

            return BrightnessLevels;
        }

        static void SetBrightness(byte targetBrightness)
        {
            //define scope (namespace)
            System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

            //define query
            System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightnessMethods");

            //output current brightness
            System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);

            System.Management.ManagementObjectCollection moc = mos.Get();

            foreach (System.Management.ManagementObject o in moc)
            {
                o.InvokeMethod("WmiSetBrightness", new Object[] { UInt32.MaxValue, targetBrightness }); //note the reversed order - won't work otherwise!
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();
        }
        #endregion





        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
