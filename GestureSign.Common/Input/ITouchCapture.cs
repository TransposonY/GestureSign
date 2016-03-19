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
        event RecognitionEventHandler GestureNotRecognized;
        void DisableTouchCapture();
        void EnableTouchCapture();
        event PointsCapturedEventHandler PointCaptured;
        bool TemporarilyDisableCapture { get; set; }
    }
}
