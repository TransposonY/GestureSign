using System;
using GestureSign.Common.Plugins;

using System.Windows.Controls;

namespace GestureSign.CorePlugins.SendKeystrokes
{
    public class SendKeystrokes : IPlugin
    {
        #region IPlugin Instance Fields

        private SendKeystrokesControl _GUI = null;

        #endregion

        #region IPlugin Instance Properties

        public string Name
        {
            get { return "发送文本"; }
        }

        public string Category
        {
            get { return "键盘"; }
        }

        public string Description
        {
            get { return "发送一段文本到程序"; }
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

                System.Windows.Forms.SendKeys.SendWait(TypedGUI.txtSendKeys.Text);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Deserialize(string SerializedData)
        {
            TypedGUI.txtSendKeys.Text = SerializedData;
        }

        public string Serialize()
        {
            return TypedGUI.txtSendKeys.Text;
        }

        #endregion

        #region Private Instance Methods

        private SendKeystrokesControl CreateGUI()
        {
            return new SendKeystrokesControl();
        }

        #endregion
    }
}
