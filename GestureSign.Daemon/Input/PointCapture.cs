using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Daemon.Filtration;
using GestureSign.PointPatterns;
using ManagedWinapi.Hooks;
using ManagedWinapi.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace GestureSign.Daemon.Input
{
    public class PointCapture : ILoadable, IPointCapture, IDisposable
    {
        #region Private Variables

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002; // Don't call back for events on installer's process
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const int GestureStackTimeout = 800;
        // Create new Touch hook control to capture global input from Touch, and create an event translator to get formal events
        private readonly PointEventTranslator _pointEventTranslator;
        private readonly InputProvider _inputProvider;
        private readonly PointerInputTargetWindow _pointerInputTargetWindow;
        private readonly List<IPointPattern> _pointPatternCache = new List<IPointPattern>();
        private readonly System.Threading.Timer _blockTouchDelayTimer;

        private System.Threading.Timer _initialTimeoutTimer;
        SynchronizationContext _currentContext;

        private Dictionary<int, List<Point>> _pointsCaptured;
        // Create variable to hold the only allowed instance of this class
        static readonly PointCapture _Instance = new PointCapture();

        private CaptureMode _mode = CaptureMode.Normal;
        private volatile CaptureState _state;

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        readonly WinEventDelegate _winEventDele;
        private readonly IntPtr _hWinEventHook;

        private bool _isGestureStackTimeout;

        private bool disposedValue = false; // To detect redundant calls

        private int? _blockTouchInputThreshold;
        private Point _touchPadStartPoint;
        private int? _lastGestureTime;

        #endregion

        #region PInvoke 

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        #endregion

        #region Public Instance Properties

        public Devices SourceDevice { get { return _pointEventTranslator.SourceDevice; } }

        public LowLevelMouseHook MouseHook
        {
            get { return _inputProvider.LowLevelMouseHook; }
        }

        public bool TemporarilyDisableCapture { get; set; }

        public List<Point>[] InputPoints
        {
            get
            {
                if (_pointsCaptured == null)
                    return new List<Point>[0];
                return _pointsCaptured.Values.ToArray();
            }
        }

        public CaptureState State
        {
            get { return _state; }
            set { _state = value; }
        }

        public CaptureMode Mode
        {
            get { return _mode; }
            set
            {
                if (value == _mode) return;
                _mode = value;
                OnModeChanged(new ModeChangedEventArgs(value));
            }
        }

        #endregion

        #region Custom Events

        public event EventHandler<IApplication[]> ForegroundApplicationsChanged;
        // Create an event to notify subscribers that CaptureState has been changed
        public event ModeChangedEventHandler ModeChanged;

        protected virtual void OnModeChanged(ModeChangedEventArgs e)
        {
            if (ModeChanged != null) ModeChanged(this, e);
        }

        // Create event to notify subscribers that the capture process has started
        public event PointsCapturedEventHandler CaptureStarted;

        protected virtual void OnCaptureStarted(PointsCapturedEventArgs e)
        {
            if (CaptureStarted != null) CaptureStarted(this, e);
        }

        // Create event to notify subscribers that a point set has been captured
        public event PointsCapturedEventHandler AfterPointsCaptured;
        public event PointsCapturedEventHandler BeforePointsCaptured;
        public event RecognitionEventHandler GestureRecognized;
        //public event RecognitionEventHandler GestureNotRecognized;

        protected virtual void OnAfterPointsCaptured(PointsCapturedEventArgs e)
        {
            if (AfterPointsCaptured != null) AfterPointsCaptured(this, e);
        }

        protected virtual void OnBeforePointsCaptured(PointsCapturedEventArgs e)
        {
            if (BeforePointsCaptured != null) BeforePointsCaptured(this, e);
        }

        protected virtual void OnGestureRecognized(RecognitionEventArgs e)
        {
            if (GestureRecognized != null) GestureRecognized(this, e);
        }

        //protected virtual void OnGestureNotRecognized(RecognitionEventArgs e)
        //{
        //    if (GestureNotRecognized != null) GestureNotRecognized(this, e);
        //}

        // Create event to notify subscribers that a single point has been captured
        public event PointsCapturedEventHandler PointCaptured;

        protected virtual void OnPointCaptured(PointsCapturedEventArgs e)
        {
            if (PointCaptured != null) PointCaptured(this, e);
        }

        // Create event to notify subscribers that the capture process has ended
        public event EventHandler CaptureEnded;

        protected virtual void OnCaptureEnded()
        {
            if (CaptureEnded != null) CaptureEnded(this, new EventArgs());
        }

        // Create event to notify subscribers that the capture has been canceled
        public event PointsCapturedEventHandler CaptureCanceled;

        protected virtual void OnCaptureCanceled(PointsCapturedEventArgs e)
        {
            if (CaptureCanceled != null) CaptureCanceled(this, e);
        }

        #endregion

        #region Public Properties

        public static PointCapture Instance
        {
            get { return _Instance; }
        }

        #endregion

        #region Constructors

        protected PointCapture()
        {
            _pointerInputTargetWindow = new PointerInputTargetWindow();
            _inputProvider = new InputProvider();
            _pointEventTranslator = new PointEventTranslator(_inputProvider);
            _pointEventTranslator.PointDown += (PointEventTranslator_PointDown);
            _pointEventTranslator.PointUp += (PointEventTranslator_PointUp);
            _pointEventTranslator.PointMove += (PointEventTranslator_PointMove);

            _currentContext = SynchronizationContext.Current;

            _winEventDele = WinEventProc;
            _hWinEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, _winEventDele, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

            if (AppConfig.UiAccess)
            {
                _blockTouchDelayTimer = new System.Threading.Timer(UpdateBlockTouchInputThresholdCallback, null, Timeout.Infinite, Timeout.Infinite);
                ForegroundApplicationsChanged += PointCapture_ForegroundApplicationsChanged;
            }

            ModeChanged += (o, e) =>
            {
                if (e.Mode == CaptureMode.UserDisabled)
                    _pointerInputTargetWindow.BlockTouchInputThreshold = 0;
            };
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        #endregion

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _initialTimeoutTimer?.Dispose();
                    _blockTouchDelayTimer?.Dispose();
                    _pointerInputTargetWindow?.Dispose();
                    _inputProvider?.Dispose();
                }

                SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                if (_hWinEventHook != IntPtr.Zero)
                    UnhookWinEvent(_hWinEventHook);

                disposedValue = true;
            }
        }

        ~PointCapture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region System Events

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_SYSTEM_FOREGROUND || eventType == EVENT_SYSTEM_MINIMIZEEND)
            {
                if (State != CaptureState.Ready || Mode != CaptureMode.Normal || hwnd.Equals(IntPtr.Zero))
                    return;
                var systemWindow = new SystemWindow(hwnd);
                if (!systemWindow.Visible)
                    return;
                var apps = ApplicationManager.Instance.GetApplicationFromWindow(systemWindow);
                ForegroundApplicationsChanged?.Invoke(this, apps);
            }
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.RemoteConnect:
                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.SessionUnlock:
                    if (State == CaptureState.Disabled)
                        State = CaptureState.Ready;
                    break;
                case SessionSwitchReason.SessionLock:
                    State = CaptureState.Disabled;
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Events

        private void PointCapture_ForegroundApplicationsChanged(object sender, IApplication[] apps)
        {
            if (apps != null)
            {
                var userAppList = apps.Where(application => application is UserApp).ToList();
                if (userAppList.Count == 0) return;
                UpdateBlockTouchInputThreshold(userAppList.Cast<UserApp>().Max(app => app.BlockTouchInputThreshold));
            }
        }

        protected void PointEventTranslator_PointDown(object sender, InputPointsEventArgs e)
        {
            if (State == CaptureState.Ready || State == CaptureState.Capturing || State == CaptureState.CapturingInvalid)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                var timeout = AppConfig.InitialTimeout;
                if (timeout > 0)
                {
                    if (_initialTimeoutTimer == null)
                    {
                        _initialTimeoutTimer = new System.Threading.Timer(InitialTimeoutCallback, null, Timeout.Infinite, Timeout.Infinite);
                    }
                    _initialTimeoutTimer.Change(timeout, Timeout.Infinite);
                }

                if (_lastGestureTime != null && Environment.TickCount - _lastGestureTime.Value > GestureStackTimeout)
                    _isGestureStackTimeout = true;

                // Try to begin capture process, if capture started then don't notify other applications of a Point event, otherwise do
                if (!TryBeginCapture(e.InputPointList))
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                }
                else e.Handled = Mode != CaptureMode.UserDisabled;
            }
        }

        protected void PointEventTranslator_PointMove(object sender, InputPointsEventArgs e)
        {
            // Only add point if we're capturing
            if (State == CaptureState.Capturing || State == CaptureState.CapturingInvalid)
            {
                AddPoint(e.InputPointList);
            }
            UpdateBlockTouchInputThreshold();
        }

        protected void PointEventTranslator_PointUp(object sender, InputPointsEventArgs e)
        {
            if (State == CaptureState.Capturing || State == CaptureState.CapturingInvalid && (SourceDevice & Devices.TouchDevice) != 0)
            {
                e.Handled = Mode != CaptureMode.UserDisabled;

                EndCapture();

                if (TemporarilyDisableCapture && Mode == CaptureMode.UserDisabled)
                {
                    TemporarilyDisableCapture = false;
                    ToggleUserDisablePointCapture();
                }
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }
            else if (State == CaptureState.CapturingInvalid && SourceDevice == Devices.Mouse)
            {
                if (Mode != CaptureMode.UserDisabled)
                {
                    State = CaptureState.Disabled;

                    var observeExceptionsTask = new Action<Task>(t =>
                    {
                        State = CaptureState.Ready;
                        Console.WriteLine($"{t.Exception.InnerException.GetType().Name}: {t.Exception.InnerException.Message}");
                    });

                    var clickAsync = Task.Factory.StartNew(delegate
                    {
                        InputSimulator simulator = new InputSimulator();
                        switch (AppConfig.DrawingButton)
                        {
                            case MouseActions.Left:
                                simulator.Mouse.LeftButtonClick();
                                break;
                            case MouseActions.Middle:
                                simulator.Mouse.MiddleButtonClick();
                                break;
                            case MouseActions.Right:
                                simulator.Mouse.RightButtonClick();
                                break;
                            case MouseActions.XButton1:
                                simulator.Mouse.XButtonClick(1);
                                break;
                            case MouseActions.XButton2:
                                simulator.Mouse.XButtonClick(2);
                                break;
                        }
                        State = CaptureState.Ready;
                    }).ContinueWith(observeExceptionsTask, TaskContinuationOptions.OnlyOnFaulted);

                    e.Handled = true;
                }
                else
                {
                    State = CaptureState.Ready;
                }
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }
            else if (State == CaptureState.TriggerFired)
            {
                State = CaptureState.Ready;
                e.Handled = Mode != CaptureMode.UserDisabled;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }

            UpdateBlockTouchInputThreshold();
            if (_initialTimeoutTimer != null)
                _initialTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #endregion

        #region Private Methods

        private void UpdateBlockTouchInputThreshold(int? threshold = null)
        {
            if (!AppConfig.UiAccess) return;

            if (threshold != null)
                _blockTouchInputThreshold = threshold;
            if (_blockTouchInputThreshold != null)
                _blockTouchDelayTimer.Change(100, Timeout.Infinite);
        }

        private void UpdateBlockTouchInputThresholdCallback(object o)
        {
            if (!_blockTouchInputThreshold.HasValue) return;

            _currentContext.Post((state) =>
            {
                _pointerInputTargetWindow.BlockTouchInputThreshold = _blockTouchInputThreshold.Value;
                _blockTouchInputThreshold = null;
            }, null);
        }

        private void InitialTimeoutCallback(object o)
        {
            _currentContext.Post((state) =>
            {
                if (State != CaptureState.CapturingInvalid) return;

                try
                {
                    if (SourceDevice == Devices.TouchScreen)
                    {
                        if (_pointerInputTargetWindow.BlockTouchInputThreshold > 1)
                            _pointerInputTargetWindow.TemporarilyDisable();
                    }
                    else if (SourceDevice == Devices.Mouse)
                    {
                        InputSimulator simulator = new InputSimulator();
                        switch (AppConfig.DrawingButton)
                        {
                            case MouseActions.Left:
                                simulator.Mouse.LeftButtonDown();
                                break;
                            case MouseActions.Middle:
                                simulator.Mouse.MiddleButtonDown();
                                break;
                            case MouseActions.Right:
                                simulator.Mouse.RightButtonDown();
                                break;
                            case MouseActions.XButton1:
                                simulator.Mouse.XButtonDown(1);
                                break;
                            case MouseActions.XButton2:
                                simulator.Mouse.XButtonDown(2);
                                break;
                        }
                    }
                    State = CaptureState.Ready;
                }
                catch
                {
                    State = CaptureState.Ready;
                }
            }, null);
        }

        private bool TryBeginCapture(List<InputPoint> firstPoint)
        {
            // Create capture args so we can notify subscribers that capture has started and allow them to cancel if they want.
            PointsCapturedEventArgs captureStartedArgs;
            if (SourceDevice == Devices.TouchPad)
            {
                _touchPadStartPoint = System.Windows.Forms.Cursor.Position;
                captureStartedArgs = new PointsCapturedEventArgs(firstPoint.Select(p => new List<Point>() { p.Point }).ToList(), new List<Point>() { _touchPadStartPoint });
            }
            else
            {
                captureStartedArgs = new PointsCapturedEventArgs(firstPoint.Select(p => p.Point).ToList());
            }
            OnCaptureStarted(captureStartedArgs);

            UpdateBlockTouchInputThreshold(Mode == CaptureMode.Normal ? captureStartedArgs.BlockTouchInputThreshold : 0);

            if (captureStartedArgs.Cancel)
                return false;

            State = CaptureState.CapturingInvalid;

            // Clear old gesture from point list so we can start adding the new captures points to the list 
            _pointsCaptured = new Dictionary<int, List<Point>>(firstPoint.Count);
            if (AppConfig.IsOrderByLocation)
            {
                foreach (var rawData in firstPoint.OrderBy(p => p.Point.X))
                {
                    if (!_pointsCaptured.ContainsKey(rawData.ContactIdentifier))
                        _pointsCaptured.Add(rawData.ContactIdentifier, new List<Point>(30));
                }
            }
            else
            {
                foreach (var rawData in firstPoint.OrderBy(p => p.ContactIdentifier))
                {
                    if (!_pointsCaptured.ContainsKey(rawData.ContactIdentifier))
                        _pointsCaptured.Add(rawData.ContactIdentifier, new List<Point>(30));
                }
            }
            AddPoint(firstPoint);
            return true;
        }

        private void EndCapture()
        {

            // Create points capture event args, to be used to send off to event subscribers or to simulate original Point event
            PointsCapturedEventArgs pointsInformation = SourceDevice == Devices.TouchPad ?
                new PointsCapturedEventArgs(_pointsCaptured.Values.ToList(), new List<Point>() { _touchPadStartPoint }) :
                new PointsCapturedEventArgs(new List<List<Point>>(_pointsCaptured.Values), _pointsCaptured.Values.Select(p => p.FirstOrDefault()).ToList());

            // Notify subscribers that capture has ended （draw end）
            OnCaptureEnded();
            State = CaptureState.Ready;

            if (_isGestureStackTimeout)
            {
                _lastGestureTime = null;
                _isGestureStackTimeout = false;
                pointsInformation.GestureTimeout = true;
            }
            // Notify PointsCaptured event subscribers that points have been captured.
            //CaptureWindow GetGestureName
            OnBeforePointsCaptured(pointsInformation);

            if (pointsInformation.Cancel) return;

            if (pointsInformation.Delay)
            {
                _lastGestureTime = Environment.TickCount;
            }
            else if (Mode == CaptureMode.Training && !(_pointsCaptured.Count == 1 && _pointsCaptured.Values.First().Count == 1))
            {
                _pointPatternCache.Clear();
                _pointPatternCache.Add(new PointPattern(new List<List<Point>>(_pointsCaptured.Values)));

                if (!NamedPipe.SendMessageAsync(_pointPatternCache.Select(p => p.Points).ToList(), "GestureSignControlPanel").Result)
                    Mode = CaptureMode.Normal;
            }

            // Fire recognized event if we found a gesture match, otherwise throw not recognized event
            if (GestureManager.Instance.GestureName != null)
                if (SourceDevice == Devices.TouchPad)
                    OnGestureRecognized(new RecognitionEventArgs(GestureManager.Instance.GestureName,
                        new List<List<Point>>() { new List<Point>() { _touchPadStartPoint } },
                        new List<Point>() { _touchPadStartPoint }, _pointsCaptured.Keys.ToList()));
                else
                    OnGestureRecognized(new RecognitionEventArgs(GestureManager.Instance.GestureName, pointsInformation.Points, pointsInformation.FirstCapturedPoints, _pointsCaptured.Keys.ToList()));
            //else
            //    OnGestureNotRecognized(new RecognitionEventArgs(pointsInformation.Points, pointsInformation.FirstCapturedPoints, _pointsCaptured.Keys.ToList()));

            OnAfterPointsCaptured(pointsInformation);

            _pointsCaptured.Clear();
        }

        //private void CancelCapture(int num)
        //{
        //    // Notify subscribers that gesture capture has been canceled
        //    OnCaptureCanceled(new PointsCapturedEventArgs(new List<List<Point>>(_pointsCaptured.Values)));
        //}

        private void AddPoint(List<InputPoint> point)
        {
            bool getNewPoint = false;
            int threshold = AppConfig.MinimumPointDistance;
            foreach (var p in point)
            {
                // Don't accept point if it's within specified distance of last point unless it's the first point
                if (_pointsCaptured.ContainsKey(p.ContactIdentifier))
                {
                    var stroke = _pointsCaptured[p.ContactIdentifier];
                    if (stroke.Count != 0)
                    {
                        if (PointPatternMath.GetDistance(stroke.Last(), p.Point) < threshold)
                            continue;

                        if (State == CaptureState.CapturingInvalid)
                            State = CaptureState.Capturing;
                    }

                    getNewPoint = true;
                    // Add point to captured points list
                    stroke.Add(p.Point);
                }
            }
            if (getNewPoint)
            {
                // Notify subscribers that point has been captured
                OnPointCaptured(new PointsCapturedEventArgs(new List<List<Point>>(_pointsCaptured.Values), point.Select(p => p.Point).ToList()));
            }
        }



        #endregion

        #region Public Methods

        public void Load()
        {
            // Shortcut method to control singleton instantiation
        }

        public void ToggleUserDisablePointCapture()
        {
            // Toggle User selected Gesture Disabling
            // Added UserDisabled to CaptureState enum since Ready and Disabled can't be used
            // due to the existing logic of Enabling/Disabling for UI/menu popup/etc.
            // The reason I had to set state to Ready if !UserDisabled was due to the sequence of the tray events.
            // I originally had to set to Disable since if you're in the popup it's disabled, however, the popup onclose
            // fires before the menu item's code, so it was back to Ready before this block was executed.  Although, it probably 
            // makes more sense to set it to Ready in the event this is called from another location.
            Mode = Mode == CaptureMode.UserDisabled ? CaptureMode.Normal : CaptureMode.UserDisabled;
        }

        #endregion
    }
}
