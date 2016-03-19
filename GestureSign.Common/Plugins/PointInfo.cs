using System;
using System.Collections.Generic;
using System.Drawing;
using ManagedWinapi.Windows;

namespace GestureSign.Common.Plugins
{
    public class PointInfo
    {
        #region Private Variables

        private List<Point> _touchLocation;
        private IntPtr _windowHandle;
        private SystemWindow _window;

        #endregion

        #region Constructors

        public PointInfo(List<Point> touchLocation, List<List<Point>> points)
        {
            _touchLocation = touchLocation;
            _window = SystemWindow.FromPointEx(_touchLocation[0].X, _touchLocation[0].Y, true, false);
            _windowHandle = _window == null ? IntPtr.Zero : _window.HWnd;
            Points = points;
        }

        #endregion

        #region Public Properties

        public List<Point> TouchLocation
        {
            get { return _touchLocation; }
            set
            {
                _touchLocation = value;
                _window = SystemWindow.FromPointEx(value[0].X, value[0].Y, true, false);
                _windowHandle = _window.HWnd;
            }
        }

        public IntPtr WindowHandle
        {
            get { return _windowHandle; }
        }

        public SystemWindow Window
        {
            get { return _window; }
        }

        public List<List<Point>> Points { get; set; }
        #endregion
    }
}