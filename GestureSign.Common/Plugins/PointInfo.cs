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
        private SystemWindow _targetWindow;
        private SynchronizationContext _syncContext;

        #endregion

        #region Constructors

        public PointInfo(List<Point> pointLocation, List<List<Point>> points, SystemWindow target, SynchronizationContext syncContext)
        {
            _pointLocation = pointLocation;
            Points = points;
            _targetWindow = target;
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

        public IntPtr WindowHandle => _targetWindow?.HWnd ?? IntPtr.Zero;

        public SystemWindow Window
        {
            get
            {
                // change target window if foreground window changed
                if (_targetWindow == null || _targetWindow.HWnd != SystemWindow.ForegroundWindow.HWnd)
                {
                    _targetWindow = SystemWindow.FromPointEx(_pointLocation[0].X, _pointLocation[0].Y, true, false);
                }
                return _targetWindow;
            }
        }

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