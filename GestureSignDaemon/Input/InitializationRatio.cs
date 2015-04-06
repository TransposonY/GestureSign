using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureSignDaemon.Input
{
    class InitializationRatio
    {
        System.Drawing.Point touchPoint;
        bool isFromGuide = false;
        MessageWindow messageWindow;
        public InitializationRatio(MessageWindow messageWindow)
        {
            this.messageWindow = messageWindow;
            MessageProcessor.OnGotTouchPoint += MessageProcessor_OnGotTouchPoint;
            messageWindow.PointsIntercepted += MessageWindow_PointsIntercepted;
            messageWindow.PointerIntercepted += MessageWindow_PointerIntercepted;
        }

        void MessageWindow_PointerIntercepted(object sender, GestureSign.Common.Input.PointerMessageEventArgs e)
        {
            touchPoint = new System.Drawing.Point(e.PointerInfo[0].PtPixelLocation.X, e.PointerInfo[0].PtPixelLocation.Y);
        }

        void MessageProcessor_OnGotTouchPoint(object sender, System.Drawing.Point e)
        {
            isFromGuide = true;
            touchPoint = e;
        }


        public void MessageWindow_PointsIntercepted(object sender, GestureSign.Common.Input.RawPointsDataMessageEventArgs e)
        {
            if (GestureSign.Common.Configuration.AppConfig.XRatio == 0 && touchPoint.X != 0)
            {
                bool XAxisDirection = false, YAxisDirection = false;
                bool IsAxisCorresponds = true;
                switch (System.Windows.Forms.SystemInformation.ScreenOrientation)
                {
                    case System.Windows.Forms.ScreenOrientation.Angle0:
                        XAxisDirection = YAxisDirection = true;
                        IsAxisCorresponds = true;
                        break;
                    case System.Windows.Forms.ScreenOrientation.Angle90:
                        IsAxisCorresponds = false;
                        XAxisDirection = false;
                        YAxisDirection = true;
                        break;
                    case System.Windows.Forms.ScreenOrientation.Angle180:
                        XAxisDirection = YAxisDirection = false;
                        IsAxisCorresponds = true;
                        break;
                    case System.Windows.Forms.ScreenOrientation.Angle270:
                        IsAxisCorresponds = false;
                        XAxisDirection = true;
                        YAxisDirection = false;
                        break;
                    default: break;
                }
                double horizontalRatio;
                double verticalRatio;
                horizontalRatio = XAxisDirection ?
                    ((double)e.RawTouchsData[0].RawPoints.X / (double)touchPoint.X) :
                    (double)e.RawTouchsData[0].RawPoints.X / (double)(System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Width - touchPoint.X);

                verticalRatio = YAxisDirection ?
                    ((double)e.RawTouchsData[0].RawPoints.Y) / (double)touchPoint.Y :
                    (double)e.RawTouchsData[0].RawPoints.Y / (double)(System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Height - touchPoint.Y);

                GestureSign.Common.Configuration.AppConfig.XRatio = IsAxisCorresponds ? horizontalRatio : verticalRatio;
                GestureSign.Common.Configuration.AppConfig.YRatio = IsAxisCorresponds ? verticalRatio : horizontalRatio;
                GestureSign.Common.Configuration.AppConfig.Save();

                if (isFromGuide)
                    GestureSign.Common.InterProcessCommunication.NamedPipe.SendMessageAsync("EndGuide", "GestureSignSetting");
                MessageProcessor.OnGotTouchPoint -= MessageProcessor_OnGotTouchPoint;
                messageWindow.PointsIntercepted -= MessageWindow_PointsIntercepted;
                messageWindow.PointerIntercepted -= MessageWindow_PointerIntercepted;
                messageWindow.Unregister();
            }
        }
    }
}
