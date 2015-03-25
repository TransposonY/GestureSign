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
        System.Drawing.Point touchPoint;

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
                        case "Guide":
                            GestureSignDaemon.Input.TouchCapture.Instance.MessageWindow.PointsIntercepted += MessageWindow_PointsIntercepted;
                            break;

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
                touchPoint = (System.Drawing.Point)data;
            }

        }


        void MessageWindow_PointsIntercepted(object sender, GestureSign.Common.Input.PointsMessageEventArgs e)
        {
            if (GestureSign.Common.Configuration.AppConfig.XRatio == 0 && touchPoint.X != 0)
            {
                bool XAxisDirection = false, YAxisDirection = false;
                switch (System.Windows.Forms.SystemInformation.ScreenOrientation)
                {
                    case System.Windows.Forms.ScreenOrientation.Angle0:
                        XAxisDirection = YAxisDirection = true;
                        break;
                    case System.Windows.Forms.ScreenOrientation.Angle90:
                        XAxisDirection = false;
                        YAxisDirection = true;
                        break;
                    case System.Windows.Forms.ScreenOrientation.Angle180:
                        XAxisDirection = YAxisDirection = false;
                        break;
                    case System.Windows.Forms.ScreenOrientation.Angle270:
                        XAxisDirection = true;
                        YAxisDirection = false;
                        break;
                    default: break;
                }
                double rateX;
                double rateY;
                rateX = XAxisDirection ?
                    ((double)e.RawTouchsData[0].RawPointsData.X / (double)touchPoint.X) :
                    (double)e.RawTouchsData[0].RawPointsData.X / (double)(System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Width - touchPoint.X);

                rateY = YAxisDirection ?
                    ((double)e.RawTouchsData[0].RawPointsData.Y) / (double)touchPoint.Y :
                    (double)e.RawTouchsData[0].RawPointsData.Y / (double)(System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Height - touchPoint.Y);

                GestureSign.Common.Configuration.AppConfig.XRatio = rateX;
                GestureSign.Common.Configuration.AppConfig.YRatio = rateY;
                GestureSign.Common.Configuration.AppConfig.Save();
                // string message = e.RawTouchsData[0].RawPointsData.X + "," + e.RawTouchsData[0].RawPointsData.Y;
                GestureSign.Common.InterProcessCommunication.NamedPipe.SendMessage("EndGuide", "GestureSignSetting");
                GestureSignDaemon.Input.TouchCapture.Instance.MessageWindow.PointsIntercepted -= MessageWindow_PointsIntercepted;

            }
        }
    }
}
