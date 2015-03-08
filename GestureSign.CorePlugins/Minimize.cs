using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Plugins;
using ManagedWinapi.Windows;

using System.Windows.Controls;

namespace GestureSign.CorePlugins
{
    public class Minimize : IPlugin
    {
        #region Private Variables

        IHostControl _HostControl = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get { return "最小化"; }
        }

        public string Description
        {
            get { return "最小化当前窗口"; }
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

        public void ShowGUI(bool IsNew)
        {
            // Nothing to do here
        }

        public bool Gestured(Common.Plugins.PointInfo ActionPoint)
        {
            try
            {
                // Don't attempt to minimize tool windows (including Windows Program Manager)
                if (ActionPoint.Window.ClassName == "Windows.UI.Core.CoreWindow" ||
                    (ActionPoint.Window.ExtendedStyle & WindowExStyleFlags.TOOLWINDOW) == WindowExStyleFlags.TOOLWINDOW)
                    return false;

                // Minimize window
                ActionPoint.Window.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            }
            catch { return false; }
            return true;
        }

        public void Deserialize(string SerializedData)
        {
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
