using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.PointPatterns;
using ManagedWinapi.Windows;
using Timer = System.Windows.Forms.Timer;

namespace GestureSign.Daemon.Input
{
    public class TouchCapture : ILoadable, ITouchCapture
    {
        #region Private Variables

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        // Create new Touch hook control to capture global input from Touch, and create an event translator to get formal events
        private readonly TouchEventTranslator _touchEventTranslator = new TouchEventTranslator();
        private readonly PointerInputTargetWindow _inputTargetWindow;
        private readonly List<IPointPattern> _pointPatternCache = new List<IPointPattern>();
        private readonly Timer _timeoutTimer = new Timer();

        Dictionary<int, List<Point>> _pointsCaptured = new Dictionary<int, List<Point>>(2);
        // Create variable to hold the only allowed instance of this class
        static readonly TouchCapture _Instance = new TouchCapture();

        private CaptureMode _mode = CaptureMode.Normal;

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        readonly WinEventDelegate _winEventDele;
        private readonly IntPtr _hWinEventHook;

        private bool _gestureTimeout;

        #endregion

        #region PInvoke 

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        #endregion

        #region Public Instance Properties

        public bool OverlayGesture { get; set; }

        // Create enumeration to identify Touch buttons
        public bool TemporarilyDisableCapture { get; set; }

        public Point[] CapturePoint
        {
            get { return _pointsCaptured.Values.Select(p => p.FirstOrDefault()).ToArray(); }
        }

        public List<Point>[] InputPoints
        {
            get
            {
                if (_pointsCaptured == null)
                    return new List<Point>[0];
                return _pointsCaptured.Values.ToArray();
            }
        }

        public CaptureState State { get; set; }

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
        public event RecognitionEventHandler GestureNotRecognized;

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

        protected virtual void OnGestureNotRecognized(RecognitionEventArgs e)
        {
            if (GestureNotRecognized != null) GestureNotRecognized(this, e);
        }

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

        public static TouchCapture Instance
        {
            get { return _Instance; }
        }

        #endregion

        #region Constructors

        protected TouchCapture()
        {
            _inputTargetWindow = new PointerInputTargetWindow();
            var inputProvider = new InputProvider();

            inputProvider.TouchInputProcessor.PointsIntercepted += _touchEventTranslator.TranslateTouchEvent;
            _touchEventTranslator.TouchDown += (PointEventTranslator_TouchDown);
            _touchEventTranslator.TouchUp += (TouchEventTranslator_TouchUp);
            _touchEventTranslator.TouchMove += (TouchEventTranslator_TouchMove);

            if (AppConfig.UiAccess)
            {
                _winEventDele = WinEventProc;
                _hWinEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _winEventDele, 0, 0, WINEVENT_OUTOFCONTEXT);
            }

            ModeChanged += (o, e) => { if (e.Mode == CaptureMode.UserDisabled) _inputTargetWindow.InterceptTouchInput(false); };

            _timeoutTimer.Tick += GestureRecognizedCallback;
        }

        #endregion

        #region Destructor

        ~TouchCapture()
        {
            if (_hWinEventHook != IntPtr.Zero)
                UnhookWinEvent(_hWinEventHook);
        }

        #endregion

        #region System Events

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (State != CaptureState.Ready || Mode != CaptureMode.Normal || hwnd.Equals(IntPtr.Zero) ||
                Application.OpenForms.Count != 0 && hwnd.Equals(Application.OpenForms[0].Handle))
                return;
            var systemWindow = new SystemWindow(hwnd);
            var userApp = ApplicationManager.Instance.GetApplicationFromWindow(systemWindow, true);
            bool flag = userApp != null &&
                        (userApp.Any(app => app is UserApplication && ((UserApplication)app).InterceptTouchInput));

            _inputTargetWindow.InterceptTouchInput(flag);
        }

        #endregion

        #region Touch Events

        protected void PointEventTranslator_TouchDown(object sender, RawPointsDataMessageEventArgs e)
        {
            // Can we begin a new gesture capture

            if (State == CaptureState.Ready || State == CaptureState.Capturing)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                // Try to begin capture process, if capture started then don't notify other applications of a Touch event, otherwise do
                if (!TryBeginCapture(e.RawTouchsData))
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                }
            }
        }
        protected void TouchEventTranslator_TouchMove(object sender, RawPointsDataMessageEventArgs e)
        {

            // Only add point if we're capturing
            if (State == CaptureState.Capturing)
            {
                if (_timeoutTimer.Enabled)
                    _timeoutTimer.Stop();

                AddPoint(e.RawTouchsData);
            }
        }

        protected void TouchEventTranslator_TouchUp(object sender, RawPointsDataMessageEventArgs e)
        {
            if (State == CaptureState.Capturing)
            {
                if (TemporarilyDisableCapture && Mode == CaptureMode.UserDisabled)
                {
                    TemporarilyDisableCapture = false;
                    ToggleUserDisableTouchCapture();
                }

                EndCapture();
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                _pointsCaptured = null;
            }

        }

        #endregion

        #region Private Methods

        private void GestureRecognizedCallback(object sender, EventArgs e)
        {
            _timeoutTimer.Stop();
            _gestureTimeout = true;
        }

        private bool TryBeginCapture(List<RawTouchData> firstTouch)
        {

            // Create capture args so we can notify subscribers that capture has started and allow them to cancel if they want.
            PointsCapturedEventArgs captureStartedArgs = new PointsCapturedEventArgs(firstTouch.Select(p => p.RawPoints).ToList());
            OnCaptureStarted(captureStartedArgs);

            _inputTargetWindow.InterceptTouchInput(captureStartedArgs.InterceptTouchInput && Mode == CaptureMode.Normal);

            if (captureStartedArgs.Cancel)
                return false;

            State = CaptureState.Capturing;

            // Clear old gesture from point list so we can start adding the new captures points to the list 
            _pointsCaptured = new Dictionary<int, List<Point>>(firstTouch.Count);
            if (AppConfig.IsOrderByLocation)
            {
                foreach (var rawTouchData in firstTouch.OrderBy(p => p.RawPoints.X))
                {
                    if (!_pointsCaptured.ContainsKey(rawTouchData.ContactIdentifier))
                        _pointsCaptured.Add(rawTouchData.ContactIdentifier, new List<Point>(30));
                }
            }
            else
            {
                foreach (var rawTouchData in firstTouch.OrderBy(p => p.ContactIdentifier))
                {
                    if (!_pointsCaptured.ContainsKey(rawTouchData.ContactIdentifier))
                        _pointsCaptured.Add(rawTouchData.ContactIdentifier, new List<Point>(30));
                }
            }
            AddPoint(firstTouch);
            return true;
        }

        private async void EndCapture()
        {

            // Create points capture event args, to be used to send off to event subscribers or to simulate original Touch event
            PointsCapturedEventArgs pointsInformation = new PointsCapturedEventArgs(new List<List<Point>>(_pointsCaptured.Values));

            // Notify subscribers that capture has ended （draw end）
            OnCaptureEnded();
            State = CaptureState.Ready;

            if (_gestureTimeout)
            {
                _gestureTimeout = false;
                pointsInformation.GestureTimeout = true;
            }
            // Notify PointsCaptured event subscribers that points have been captured.
            //CaptureWindow GetGestureName
            OnBeforePointsCaptured(pointsInformation);

            if (pointsInformation.Cancel) return;

            if (pointsInformation.Delay)
            {
                _timeoutTimer.Interval = AppConfig.GestureTimeout;
                _timeoutTimer.Start();
            }
            else if (Mode == CaptureMode.Training && !(_pointsCaptured.Count == 1 && _pointsCaptured.Values.First().Count == 1))
            {
                try
                {
                    bool createdSetting;
                    using (new Mutex(false, "GestureSignControlPanel", out createdSetting)) { }
                    if (createdSetting)
                    {
                        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSign.exe");
                        if (File.Exists(path))
                            using (Process daemon = new Process())
                            {
                                daemon.StartInfo.FileName = path;
                                daemon.StartInfo.Arguments = "/L";
                                // pipeClient.StartInfo.Arguments =            
                                //daemon.StartInfo.UseShellExecute = false;
                                daemon.Start();
                                daemon.WaitForInputIdle();
                            }
                    }
                }
                catch (Exception exception)
                {
                    Logging.LogException(exception);
                    MessageBox.Show(exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                if (OverlayGesture)
                {
                    OverlayGesture = false;
                }
                else
                {
                    _pointPatternCache.Clear();
                }
                _pointPatternCache.Add(new PointPattern(new List<List<Point>>(_pointsCaptured.Values)));

                var message = new Tuple<string, List<List<List<Point>>>>(GestureManager.Instance.GestureName, _pointPatternCache.Select(p => p.Points).ToList());
                if (await NamedPipe.SendMessageAsync(message, "GestureSignControlPanel"))
                    DisableTouchCapture();
            }

            // Fire recognized event if we found a gesture match, otherwise throw not recognized event
            if (GestureManager.Instance.GestureName != null)
                OnGestureRecognized(new RecognitionEventArgs(GestureManager.Instance.GestureName, pointsInformation.Points, pointsInformation.LastCapturedPoints));
            else
                OnGestureNotRecognized(new RecognitionEventArgs(pointsInformation.Points, pointsInformation.LastCapturedPoints));

            OnAfterPointsCaptured(pointsInformation);

        }

        private void CancelCapture(int num)
        {
            // Notify subscribers that gesture capture has been canceled
            OnCaptureCanceled(new PointsCapturedEventArgs(new List<List<Point>>(_pointsCaptured.Values)));
        }

        private void AddPoint(List<RawTouchData> point)
        {
            bool getNewPoint = false;
            foreach (RawTouchData p in point)
            {                // Don't accept point if it's within specified distance of last point unless it's the first point
                if (_pointsCaptured.ContainsKey(p.ContactIdentifier))
                {
                    var stroke = _pointsCaptured[p.ContactIdentifier];
                    if (stroke.Count != 0 && PointPatternMath.GetDistance(stroke.Last(), p.RawPoints) < AppConfig.MinimumPointDistance)
                        continue;
                    getNewPoint = true;
                    // Add point to captured points list
                    stroke.Add(p.RawPoints);
                }
            }
            if (getNewPoint)
            {

                // Notify subscribers that point has been captured
                OnPointCaptured(new PointsCapturedEventArgs(new List<List<Point>>(_pointsCaptured.Values), point.Select(p => p.RawPoints).ToList()));
            }
        }



        #endregion

        #region Public Methods

        public void Load()
        {
            // Shortcut method to control singleton instantiation
        }

        public void EnableTouchCapture()
        {
            State = CaptureState.Ready;
        }

        public void DisableTouchCapture()
        {
            State = CaptureState.Disabled;
        }

        public void ToggleUserDisableTouchCapture()
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
