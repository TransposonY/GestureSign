using System;
using GestureSign.Common.Plugins;

using System.Windows.Controls;
using GestureSign.Common.Localization;

namespace GestureSign.CorePlugins.SendKeystrokes
{
    public class SendKeystrokes : IPlugin
    {
        #region IPlugin Instance Fields

        private SendKeystrokesControl _GUI = null;
        private string _keystrokes;

        #endregion

        #region IPlugin Instance Properties

        public string Name
        {
            get { return LanguageDataManager.Instance.GetTextValue("CorePlugins.SendKeystrokes.Name"); }
        }

        public string Category
        {
            get { return LanguageDataManager.Instance.GetTextValue("CorePlugins.SendKeystrokes.Category"); }
        }

        public string Description
        {
            get { return LanguageDataManager.Instance.GetTextValue("CorePlugins.SendKeystrokes.Description"); }
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
                if (ActionPoint.WindowHandle.ToInt64() != ManagedWinapi.Windows.SystemWindow.ForegroundWindow.HWnd.ToInt64())
                    ManagedWinapi.Windows.SystemWindow.ForegroundWindow = ActionPoint.Window;

                System.Windows.Forms.SendKeys.SendWait(_keystrokes);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Deserialize(string SerializedData)
        {
            _keystrokes = SerializedData;
            return true;
        }

        public string Serialize()
        {
            if (_GUI != null)
            {
                _keystrokes = _GUI.txtSendKeys.Text;
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
                TypedGUI.txtSendKeys.Text = _keystrokes;
            };
            return sendKeystrokesControl;
        }

        #endregion
    }
}
