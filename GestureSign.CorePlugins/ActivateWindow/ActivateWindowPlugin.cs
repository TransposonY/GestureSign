using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using GestureSign.Common.Localization;
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
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.ActivateWindow.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.ActivateWindow.Description"); }
        }

        public object GUI
        {
            get { return _gui ?? (_gui = CreateGUI()); }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
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

        public object Icon => IconSource.Window;

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo actionPoint)
        {
            var tick = Environment.TickCount;
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
                        var window = new SystemWindow(hWnd);
                        if (window.WindowState == System.Windows.Forms.FormWindowState.Minimized)
                        {
                            window.RestoreWindow();
                        }
                        SystemWindow.ForegroundWindow = window;
                        return true;
                    }
                }
                Thread.Sleep(10);
            } while (_settings.Timeout > 0 && Environment.TickCount - tick < _settings.Timeout);
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

                        if (window.WindowState == System.Windows.Forms.FormWindowState.Minimized)
                        {
                            window.RestoreWindow();
                        }

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
            return PluginHelper.DeserializeSettings(SerializedData, out _settings);
        }

        public string Serialize()
        {
            if (_gui != null)
                _settings = _gui.Settings;

            if (_settings == null)
                _settings = new ActivateWindowSettings();

            return PluginHelper.SerializeSettings(_settings);
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