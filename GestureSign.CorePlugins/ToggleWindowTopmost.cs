using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins
{
    class ToggleWindowTopmost : IPlugin
    {
        #region Private Variables

        IHostControl _HostControl = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.ToggleWindowTopmost.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.ToggleWindowTopmost.Description"); }
        }

        public UserControl GUI
        {
            get { return null; }
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
            if (actionPoint.WindowHandle.ToInt64() != ManagedWinapi.Windows.SystemWindow.ForegroundWindow.HWnd.ToInt64())
                ManagedWinapi.Windows.SystemWindow.ForegroundWindow = actionPoint.Window;

            actionPoint.Window.TopMost = !actionPoint.Window.TopMost;

            return true;
        }

        public bool Deserialize(string serializedData)
        {
            return true;
            // Nothing to do here
        }

        public string Serialize()
        {
            // Nothing to serialize
            return "";
        }

        #endregion

        #region Host Control

        public IHostControl HostControl
        {
            get { return _HostControl; }
            set { _HostControl = value; }
        }

        #endregion
    }
}
