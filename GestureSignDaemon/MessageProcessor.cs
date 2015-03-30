using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using System.Runtime.Serialization.Formatters.Binary;
namespace GestureSignDaemon
{
    class MessageProcessor
    {
        internal static event EventHandler<System.Drawing.Point> OnGotTouchPoint;
        public void ProcessMessages(System.IO.Pipes.NamedPipeServerStream server)
        {
            BinaryFormatter binForm = new BinaryFormatter();
            object data = binForm.Deserialize(server);
            if (data is string)
            {
                string message = data.ToString();/// sr.ReadLine();
                GestureSignDaemon.Input.TouchCapture.Instance.MessageWindow.Invoke(new Action(() =>
                {
                    switch (message)
                    {
                        //case "Guide":
                        //    GestureSignDaemon.Input.TouchCapture.Instance.MessageWindow.PointsIntercepted += InitializationRatio.MessageWindow_PointsIntercepted;
                        //    break;
                        case "StartTeaching":
                            {
                                if (Input.TouchCapture.Instance.State == GestureSign.Common.Input.CaptureState.UserDisabled)
                                    GestureSignDaemon.TrayManager.Instance.ToggleDisableGestures();
                                if (!GestureSign.Common.Configuration.AppConfig.Teaching)
                                    GestureSignDaemon.TrayManager.Instance.StartTeaching();
                                break;
                            }
                        case "EnableTouchCapture":
                            Input.TouchCapture.Instance.EnableTouchCapture();
                            break;
                        case "DisableTouchCapture":
                            Input.TouchCapture.Instance.DisableTouchCapture();
                            break;
                    }
                }));
            }
            else if (data is System.Drawing.Point)
            {
                if (OnGotTouchPoint != null)
                {
                    OnGotTouchPoint(this, (System.Drawing.Point)data);
                }
            }

        }

    }
}
