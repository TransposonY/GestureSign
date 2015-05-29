using System.Drawing;
using System.Windows.Forms;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;

namespace GestureSignDaemon.Input
{
    class RangeInitialization
    {  
        Point _touchPoint;
        bool _isFromGuide;
        readonly MessageWindow _messageWindow;
        public RangeInitialization(MessageWindow messageWindow)
        {
            _messageWindow = messageWindow;
            MessageProcessor.OnGotTouchPoint += MessageProcessor_OnGotTouchPoint;
            messageWindow.PointsIntercepted += MessageWindow_PointsIntercepted;
            messageWindow.PointerIntercepted += MessageWindow_PointerIntercepted;
        }

        void MessageWindow_PointerIntercepted(object sender, PointerMessageEventArgs e)
        {
            _touchPoint = new Point(e.PointerInfo[0].PtPixelLocation.X, e.PointerInfo[0].PtPixelLocation.Y);
        }

        void MessageProcessor_OnGotTouchPoint(object sender, Point e)
        {
            _isFromGuide = true;
            _touchPoint = e;
        }


        public void MessageWindow_PointsIntercepted(object sender, RawPointsDataMessageEventArgs e)
        {
            if (AppConfig.XRange == 0 && _touchPoint.X != 0)
            {
                bool xAxisDirection = false, yAxisDirection = false;
                bool isAxisCorresponds = true;
                switch (SystemInformation.ScreenOrientation)
                {
                    case ScreenOrientation.Angle0:
                        xAxisDirection = yAxisDirection = true;
                        isAxisCorresponds = true;
                        break;
                    case ScreenOrientation.Angle90:
                        isAxisCorresponds = false;
                        xAxisDirection = false;
                        yAxisDirection = true;
                        break;
                    case ScreenOrientation.Angle180:
                        xAxisDirection = yAxisDirection = false;
                        isAxisCorresponds = true;
                        break;
                    case ScreenOrientation.Angle270:
                        isAxisCorresponds = false;
                        xAxisDirection = true;
                        yAxisDirection = false;
                        break;
                    default: break;
                }
                var horizontalRange = SystemInformation.PrimaryMonitorSize.Width*e.RawTouchsData[0].RawPoints.X/
                                      ( xAxisDirection ?_touchPoint.X :SystemInformation.PrimaryMonitorSize.Width - _touchPoint.X);

                var verticalRange = SystemInformation.PrimaryMonitorSize.Height*e.RawTouchsData[0].RawPoints.Y /
                                    ( yAxisDirection ?_touchPoint.Y :SystemInformation.PrimaryMonitorSize.Height - _touchPoint.Y);

                AppConfig.XRange = isAxisCorresponds ? horizontalRange : verticalRange;
                AppConfig.YRange = isAxisCorresponds ? verticalRange : horizontalRange;
                AppConfig.Save();

                if (_isFromGuide)
                    NamedPipe.SendMessageAsync("InitializationCompleted", "GestureSignSetting");
                MessageProcessor.OnGotTouchPoint -= MessageProcessor_OnGotTouchPoint;
                _messageWindow.PointsIntercepted -= MessageWindow_PointsIntercepted;
                _messageWindow.PointerIntercepted -= MessageWindow_PointerIntercepted;
                _messageWindow.Unregister();
            }
        }
    }
}
