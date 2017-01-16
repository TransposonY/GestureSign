using System;
using System.Collections.Generic;
using System.Drawing;
using ManagedWinapi.Windows;

namespace GestureSign.Common.Plugins
{
    public class PointInfo
    {
        #region Private Variables

        private List<Point> _pointLocation;
        private SystemWindow _window;

        #endregion

        #region Constructors

        public PointInfo(List<Point> pointLocation, List<List<Point>> points)
        {
            _pointLocation = pointLocation;
            Points = points;
        }

        #endregion

        #region Public Properties

        public List<Point> PointLocation
        {
            get { return _pointLocation; }
            set
            {
                _pointLocation = value;
            }
        }

        public IntPtr WindowHandle => _window?.HWnd ?? IntPtr.Zero;

        public SystemWindow Window => _window ?? (_window = SystemWindow.FromPointEx(_pointLocation[0].X, _pointLocation[0].Y, true, false));

        public List<List<Point>> Points { get; set; }

        #endregion
    }
}