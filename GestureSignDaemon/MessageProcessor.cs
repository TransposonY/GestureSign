using System;
using System.Drawing;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSignDaemon.Input;

namespace GestureSignDaemon
{
    class MessageProcessor
    {
        internal static event EventHandler<Point> OnGotTouchPoint;
        public void ProcessMessages(NamedPipeServerStream server)
        {
            BinaryFormatter binForm = new BinaryFormatter();
            object data = binForm.Deserialize(server);
            if (data is string)
            {
                var message = data.ToString();
                TouchCapture.Instance.MessageWindow.Invoke(new Action(() =>
                {
                    switch (message)
                    {
                        //case "Guide":
                        //    GestureSignDaemon.Input.TouchCapture.Instance.MessageWindow.PointsIntercepted += InitializationRatio.MessageWindow_PointsIntercepted;
                        //    break;
                        case "StartTeaching":
                            {
                                if (TouchCapture.Instance.State == CaptureState.UserDisabled)
                                    TrayManager.Instance.ToggleDisableGestures();
                                if (!AppConfig.Teaching)
                                    TrayManager.Instance.StartTeaching();
                                break;
                            }
                        case "EnableTouchCapture":
                            TouchCapture.Instance.EnableTouchCapture();
                            break;
                        case "DisableTouchCapture":
                            TouchCapture.Instance.DisableTouchCapture();
                            break;
                        case "LoadApplications":
                            GestureSign.Common.Applications.ApplicationManager.Instance.LoadApplications();
                            break;
                        case "LoadGestures":
                            GestureSign.Common.Gestures.GestureManager.Instance.LoadGestures();
                            break;
                    }
                }));
            }
            else if (data is Point)
            {
                if (OnGotTouchPoint != null)
                {
                    OnGotTouchPoint(this, (Point)data);
                }
            }

        }

    }
}
