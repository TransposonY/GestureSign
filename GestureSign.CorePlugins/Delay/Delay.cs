using System;
using GestureSign.Common.Plugins;
using GestureSign.Common.Localization;

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
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.Delay.Name"); }
        }

        public string Category
        {
            get { return "GestureSign"; }
        }

        public string Description
        {
            get { return String.Format(LocalizationProvider.Instance.GetTextValue("CorePlugins.Delay.Description"), _timeout); }
        }

        public bool IsAction
        {
            get { return true; }
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

        public DelayUI TypedGUI
        {
            get { return (DelayUI)GUI; }
        }

        public IHostControl HostControl { get; set; }

        public object Icon => IconSource.GestureSign;

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
