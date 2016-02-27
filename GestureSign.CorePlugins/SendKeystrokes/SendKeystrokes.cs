using System;
using GestureSign.Common.Plugins;

using System.Windows.Controls;
using WindowsInput;
using GestureSign.Common.Localization;

namespace GestureSign.CorePlugins.SendKeystrokes
{
    public class SendKeystrokes : IPlugin
    {
        #region IPlugin Instance Fields

        private SendKeystrokesControl _GUI = null;
        private string _keystrokes;
        private bool _useSendInput;
        private const string SendInput = "{SendInput}";

        #endregion

        #region IPlugin Instance Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.SendKeystrokes.Name"); }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.SendKeystrokes.Category"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.SendKeystrokes.Description"); }
        }

        public bool IsAction
        {
            get { return true; }
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

        public SendKeystrokesControl TypedGUI
        {
            get { return (SendKeystrokesControl)GUI; }
        }

        public IHostControl HostControl { get; set; }

        #endregion

        #region IPlugin Instance Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo ActionPoint)
        {
            try
            {
                if (ActionPoint.WindowHandle.ToInt64() !=
                    ManagedWinapi.Windows.SystemWindow.ForegroundWindow.HWnd.ToInt64())
                    ManagedWinapi.Windows.SystemWindow.ForegroundWindow = ActionPoint.Window;
                if (_useSendInput)
                {
                    InputSimulator simulator = new InputSimulator();
                    simulator.Keyboard.TextEntry(_keystrokes);
                }
                else
                {
                    System.Windows.Forms.SendKeys.SendWait(_keystrokes);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Deserialize(string SerializedData)
        {
            _useSendInput = SerializedData.StartsWith(SendInput);
            _keystrokes = _useSendInput ? SerializedData.Remove(0, SendInput.Length) : SerializedData;
            return true;
        }

        public string Serialize()
        {
            if (_GUI != null)
            {
                _keystrokes = _GUI.TxtSendKeys.Text;
                if (_GUI.UseSendInputCheckBox.IsChecked.Value)
                    return _keystrokes.Insert(0, SendInput);
                else
                    return _keystrokes;
            }
            else return _keystrokes ?? String.Empty;
        }

        #endregion

        #region Private Instance Methods

        private SendKeystrokesControl CreateGUI()
        {
            SendKeystrokesControl sendKeystrokesControl = new SendKeystrokesControl();
            sendKeystrokesControl.Loaded += (s, o) =>
            {
                TypedGUI.UseSendInputCheckBox.IsChecked = _useSendInput;
                TypedGUI.TxtSendKeys.Text = _keystrokes;
            };
            return sendKeystrokesControl;
        }

        #endregion
    }
}
