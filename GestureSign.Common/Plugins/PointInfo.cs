using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using ManagedWinapi.Windows;

namespace GestureSign.Common.Plugins
{
    public class PointInfo
    {
        #region Private Variables

        private List<Point> _pointLocation;
        private SystemWindow _window;
        private SynchronizationContext _syncContext;

        #endregion

        #region Constructors

        public PointInfo(List<Point> pointLocation, List<List<Point>> points, SynchronizationContext syncContext)
        {
            _pointLocation = pointLocation;
            Points = points;
            _syncContext = syncContext;
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

        #region Public Methods

        public void Invoke(Action action)
        {
            _syncContext.Send((o) => action.Invoke(), null);
        }

        #endregion
    }
}