using System;
using GestureSign.Common.Plugins;

using System.Windows.Controls;

namespace GestureSign.CorePlugins.Delay
{
    public class Delay : IPlugin
    {
        #region IPlugin Instance Fields

        private DelayUI _GUI = null;
        private int _timeout;

        #endregion

        #region IPlugin Instance Properties

        public string Name
        {
            get { return "延时"; }
        }

        public string Category
        {
            get { return "GestureSign"; }
        }

        public string Description
        {
            get { return String.Format("延迟 {0} 毫秒", _timeout); }
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

        public DelayUI TypedGUI
        {
            get { return (DelayUI)GUI; }
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
                System.Threading.Thread.Sleep(_timeout);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Deserialize(string SerializedData)
        {
            return int.TryParse(SerializedData, out _timeout);
        }

        public string Serialize()
        {
            if (_GUI != null)
            {
                _timeout = _GUI.Timeout;
            }
            return _timeout.ToString();
        }

        #endregion

        #region Private Instance Methods

        private DelayUI CreateGUI()
        {
            DelayUI delayUI = new DelayUI();
            delayUI.Loaded += (s, o) =>
            {
                TypedGUI.Timeout = _timeout;
            };
            return delayUI;
        }

        #endregion
    }
}
