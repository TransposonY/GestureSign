using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GestureSign.Common.Plugins
{
    public class PointInfo
    {
        #region Private Variables

        private Point _TouchLocation;
        private IntPtr _WindowHandle;
        private ManagedWinapi.Windows.SystemWindow _Window;

        #endregion

        #region Constructors

        public PointInfo(PointF TouchLocation)
        {
            _TouchLocation = Point.Round(TouchLocation);
            _Window = ManagedWinapi.Windows.SystemWindow.FromPointEx(_TouchLocation.X, _TouchLocation.Y, true, false);
            if (_Window == null) _WindowHandle = IntPtr.Zero;
            else _WindowHandle = _Window.HWnd;
        }

        #endregion

        #region Public Properties

        public Point TouchLocation
        {
            get { return _TouchLocation; }
            set
            {
                _TouchLocation = value;
                _Window = ManagedWinapi.Windows.SystemWindow.FromPointEx(value.X, value.Y, true, false);
                _WindowHandle = _Window.HWnd;
            }
        }

        public IntPtr WindowHandle
        {
            get { return _WindowHandle; }
        }

        public ManagedWinapi.Windows.SystemWindow Window
        {
            get { return _Window; }
        }

        #endregion
    }
}