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

        public object GUI
        {
            get { return null; }
        }

        public bool ActivateWindowDefault
        {
            get { return true; }
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
