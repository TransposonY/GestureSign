using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using GestureSign.Common.Plugins;
using ManagedWinapi.Windows;

namespace GestureSign.CorePlugins.ActivateWindow
{
    public class ActivateWindowPlugin : IPlugin
    {
        #region Private Variables

        private ActivateWindowUI _gui;
        private ActivateWindowSettings _settings;
        private delegate bool CallBackEnumWindowsProc(IntPtr hWnd, int lParam);

        private bool _isFound;

        private const string User32 = "user32.dll";
        #endregion

        #region PInvoke Declarations
        [DllImport(User32)]
        private static extern int EnumWindows(CallBackEnumWindowsProc ewp, int lParam);

        [DllImport(User32)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport(User32)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        #endregion

        #region Public Properties

        public string Name
        {
            get { return "激活窗口"; }
        }

        public string Description
        {
            get { return "使指定窗口前置"; }
        }

        public UserControl GUI
        {
            get { return _gui ?? (_gui = CreateGUI()); }
        }

        public ActivateWindowUI TypedGUI
        {
            get { return (ActivateWindowUI)GUI; }
        }

        public string Category
        {
            get { return "Windows"; }
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

        public bool Gestured(PointInfo actionPoint)
        {
            Stopwatch sw = Stopwatch.StartNew();

            do
            {
                if (_settings.IsRegEx)
                {
                    CallBackEnumWindowsProc ewp = EnumWindowsProc;
                    EnumWindows(ewp, 0);
                    if (_isFound) return true;
                }
                else
                {
                    string className = String.IsNullOrWhiteSpace(_settings.ClassName) ? null : _settings.ClassName;
                    string caption = String.IsNullOrWhiteSpace(_settings.Caption) ? null : _settings.Caption;
                    IntPtr hWnd = FindWindow(className, caption);
                    if (hWnd != IntPtr.Zero)
                    {
                        SystemWindow.ForegroundWindow = new SystemWindow(hWnd);
                        sw.Stop();
                        return true;
                    }
                }
                Thread.Sleep(10);
            } while (sw.ElapsedMilliseconds < _settings.Timeout);
            sw.Stop();
            return false;
        }

        private bool EnumWindowsProc(IntPtr hWnd, int lParam)
        {
            if (IsWindowVisible(hWnd))
            {
                try
                {
                    var window = new SystemWindow(hWnd);

                    if (
                        Regex.IsMatch(window.Title, _settings.Caption,
                        RegexOptions.Singleline | RegexOptions.IgnoreCase) &&
                        Regex.IsMatch(window.ClassName, _settings.ClassName,
                        RegexOptions.Singleline | RegexOptions.IgnoreCase))
                    {
                        _isFound = true;
                        SystemWindow.ForegroundWindow = window;
                        return false;
                    }
                }
                catch
                {
                    _isFound = true;
                    return false;
                }
            }
            _isFound = false;
            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            // Clear existing settings if nothing was passed in
            if (String.IsNullOrEmpty(SerializedData))
            {
                _settings = new ActivateWindowSettings();
                return true;
            }
            try
            {
                // Create memory stream from serialized data string
                MemoryStream memStream = new MemoryStream(Encoding.Default.GetBytes(SerializedData));

                // Create json serializer to deserialize json file
                DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(ActivateWindowSettings));

                // Deserialize json file into actions list
                _settings = jSerial.ReadObject(memStream) as ActivateWindowSettings;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            if (_settings == null)
                _settings = new ActivateWindowSettings();
            return true;
        }

        public string Serialize()
        {
            if (_gui != null)
                _settings = _gui.Settings;

            if (_settings == null)
                _settings = new ActivateWindowSettings();

            // Create json serializer to serialize json file
            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(ActivateWindowSettings));

            // Open json file
            MemoryStream mStream = new MemoryStream();

            // Serialize actions into json file
            jSerial.WriteObject(mStream, _settings);

            return Encoding.Default.GetString(mStream.ToArray());
        }

        #endregion

        #region Private Methods

        private ActivateWindowUI CreateGUI()
        {
            ActivateWindowUI newGUI = new ActivateWindowUI() { Settings = _settings };
            newGUI.Loaded += (o, e) => { TypedGUI.Settings = _settings; };

            return newGUI;
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}