using System;
namespace GestureSign.Common.UI
{
	public interface ITrayManager
	{
		//void EnterUserDefinedMode();
		void StartTeaching();
		void StopTeaching();
		void ToggleDisableGestures();
		void ToggleTeaching();
	}
}
