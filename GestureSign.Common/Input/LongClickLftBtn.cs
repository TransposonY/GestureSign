using GestureSign.Common.Configuration;
using System;
using System.Threading;

namespace GestureSign.Common.Input
{
    public class LongClickLftBtn
    {
        volatile public static bool useFeature = AppConfig.LongClickLftBtn;
        public static int? fingerId = null;
        volatile public static int downX = 0;
        volatile public static int downY = 0;
        volatile public static bool waitTimer = false;
        volatile public static Timer timer = null;
        volatile public static bool needRelease = false;
        //ms
        volatile public static int INTERVAL = 315+AppConfig.InitialTimeout;
        //px
        public const int MOVE_THRESOLD = 225;

        public static  void Reset()
        {
            fingerId = null;
            downX = 0;
            downY = 0;
            waitTimer = false;
            timer?.Dispose();
            timer = null;
            needRelease = false;
        }
    }
}
