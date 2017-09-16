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
        bool TemporarilyDisableCapture { get; set; }
        Devices SourceDevice { get; set; }
        CaptureState State { get; set; }
        CaptureMode Mode { get; set; }
    }
}
