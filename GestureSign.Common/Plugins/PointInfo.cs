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
        private SystemWindow _window;

        #endregion

        #region Constructors

        public PointInfo(List<Point> touchLocation, List<List<Point>> points)
        {
            _touchLocation = touchLocation;
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
            }
        }

        public IntPtr WindowHandle => _window?.HWnd ?? IntPtr.Zero;

        public SystemWindow Window => _window ?? (_window = SystemWindow.FromPointEx(_touchLocation[0].X, _touchLocation[0].Y, true, false));

        public List<List<Point>> Points { get; set; }

        #endregion
    }
}