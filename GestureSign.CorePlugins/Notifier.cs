using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Plugins;
using System.Windows.Controls;
namespace GestureSign.CorePlugins
{
	public class Notifier : IPlugin
	{
		#region IPlugin Members

		public string Name
		{
			get { return "提示"; }
		}

		public string Description
		{
			get { return "提示用户所执行的动作"; }
		}

        public UserControl GUI
		{
			get { return null; }
		}

		public string Category
		{
			get { return "Common"; }
		}

		public bool IsAction
		{
			get { return false; }
		}

		public void Initialize()
		{
            //HostControl.GestureManager.GestureRecognized += (o, e) =>
            //    {
            //        //MessageBox.Show(String.Format("You drew a '{0}'", e.GestureName));
            //    };
		}

		public bool Gestured(PointInfo ActionPoint)
		{
			return true;
		}

		public bool Deserialize(string SerializedData)
        {
            return true;			
		}

		public string Serialize()
		{
			return String.Empty;
		}

		public IHostControl HostControl { get; set; }

		#endregion
	}
}
