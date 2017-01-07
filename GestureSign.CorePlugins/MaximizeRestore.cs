using GestureSign.Common.Plugins;
using GestureSign.Common.Localization;

namespace GestureSign.CorePlugins
{
	public class MaximizeRestore : IPlugin
	{
		#region Private Variables

		IHostControl _HostControl = null;

		#endregion

		#region Public Properties

		public string Name
		{
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.MaximizeRestore.Name"); }
		}

		public string Description
		{
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.MaximizeRestore.Description"); }
		}

		public object GUI
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
			if (ActionPoint.WindowHandle.ToInt64() != ManagedWinapi.Windows.SystemWindow.ForegroundWindow.HWnd.ToInt64())
				ManagedWinapi.Windows.SystemWindow.ForegroundWindow = ActionPoint.Window;

			// Toggle window state
			if (ActionPoint.Window.WindowState == System.Windows.Forms.FormWindowState.Maximized)
				ActionPoint.Window.WindowState = System.Windows.Forms.FormWindowState.Normal;
			else
				ActionPoint.Window.WindowState = System.Windows.Forms.FormWindowState.Maximized;

			return true;
		}

		public bool Deserialize(string SerializedData)
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
