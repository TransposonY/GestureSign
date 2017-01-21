using System;

namespace GestureSign.Common.Input
{
    public interface IPointCapture
    {
        event PointsCapturedEventHandler AfterPointsCaptured;
        event PointsCapturedEventHandler BeforePointsCaptured;
        event PointsCapturedEventHandler CaptureStarted;
        event EventHandler CaptureEnded;
        event RecognitionEventHandler GestureRecognized;
        void DisablePointCapture();
        void EnablePointCapture();
        event PointsCapturedEventHandler PointCaptured;
        bool StackUpGesture { get; set; }
        bool TemporarilyDisableCapture { get; set; }
        bool MouseCaptured { get; set; }
        CaptureState State { get; set; }
        CaptureMode Mode { get; set; }
    }
}
