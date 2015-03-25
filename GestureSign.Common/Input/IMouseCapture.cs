using System;

namespace GestureSign.Common.Input
{
	public interface ITouchCapture
	{
		event PointsCapturedEventHandler AfterPointsCaptured;
		event PointsCapturedEventHandler BeforePointsCaptured;
        event PointsCapturedEventHandler CaptureStarted;
		event EventHandler CaptureEnded;
		void DisableTouchCapture();
		void EnableTouchCapture();
		event PointsCapturedEventHandler PointCaptured;
        IntPtr MessageWindowHandle { get; }
	}
}
