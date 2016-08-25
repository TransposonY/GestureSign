using System;

namespace GestureSign.Common.Input
{
    public interface ITouchCapture
    {
        event PointsCapturedEventHandler AfterPointsCaptured;
        event PointsCapturedEventHandler BeforePointsCaptured;
        event PointsCapturedEventHandler CaptureStarted;
        event EventHandler CaptureEnded;
        event RecognitionEventHandler GestureRecognized;
        void DisableTouchCapture();
        void EnableTouchCapture();
        event PointsCapturedEventHandler PointCaptured;
        bool StackUpGesture { get; set; }
        bool TemporarilyDisableCapture { get; set; }
        CaptureState State { get; set; }
        CaptureMode Mode { get; set; }
    }
}
