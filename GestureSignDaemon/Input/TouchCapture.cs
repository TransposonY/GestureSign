using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GestureSign.Common;
using GestureSign.Common.Input;
using GestureSign.PointPatterns;
using System.Diagnostics;

namespace GestureSignDaemon.Input
{
    public class TouchCapture : ILoadable, ITouchCapture
    {
        #region Private Variables

        // Create new Touch hook control to capture global input from Touch, and create an event translator to get formal events
        TouchEventTranslator TouchEventTranslator = new TouchEventTranslator();
        Input.MessageWindow messageWindow = new MessageWindow();

        Dictionary<int, List<Point>> _PointsCaptured = new Dictionary<int, List<Point>>(2);
        // Create variable to hold the only allowed instance of this class
        static readonly TouchCapture _Instance = new TouchCapture();

        // Create enumeration to identify Touch buttons
        public IntPtr MessageWindowHandle { get { return messageWindow.Handle; } }
        public MessageWindow MessageWindow { get { return messageWindow; } }

        #endregion

        #region Public Instance Properties

        public Point[] CapturePoint
        {
            get { return _PointsCaptured.Values.Select(p => p.FirstOrDefault()).ToArray(); }
        }

        public List<Point>[] InputPoints
        {
            get
            {
                if (_PointsCaptured == null)
                    return new List<Point>[0];
                return _PointsCaptured.Values.ToArray();
            }
        }

        public CaptureState State { get; private set; }

        #endregion

        #region Custom Events

        // Create an event to notify subscribers that CaptureState has been changed
        public event StateChangedEventHandler StateChanged;

        protected virtual void OnStateChanged(StateChangedEventArgs e)
        {
            StateChanged(this, e);
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

        protected virtual void OnAfterPointsCaptured(PointsCapturedEventArgs e)
        {
            if (AfterPointsCaptured != null) AfterPointsCaptured(this, e);
        }

        protected virtual void OnBeforePointsCaptured(PointsCapturedEventArgs e)
        {
            if (BeforePointsCaptured != null) BeforePointsCaptured(this, e);
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

        public event EventHandler StartInterceptTouchInput;
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
            messageWindow.PointsIntercepted += new RawPointsDataMessageEventHandler(TouchEventTranslator.TranslateTouchEvent);
            TouchEventTranslator.TouchDown += (PointEventTranslator_TouchDown);
            TouchEventTranslator.TouchUp += (TouchEventTranslator_TouchUp);
            TouchEventTranslator.TouchMove += (TouchEventTranslator_TouchMove);

            messageWindow.PointerIntercepted += TouchEventTranslator.TranslatePointerMessage;
            this.StartInterceptTouchInput += messageWindow.StartRegister;
        }


        #endregion

        #region Touch Events


        protected void PointEventTranslator_TouchDown(object sender, PointEventArgs e)
        {
            // Can we begin a new gesture capture

            if (State == CaptureState.Ready || State == CaptureState.Capturing)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                //clear surface
                OnCaptureEnded();

                // Try to begin capture process, if capture started then don't notify other applications of a Touch event, otherwise do

                if (!TryBeginCapture(e.Points))
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                    return;
                }
            }
        }
        protected void TouchEventTranslator_TouchMove(object sender, PointEventArgs e)
        {

            // Only add point if we're capturing
            if (State == CaptureState.Capturing)
            {
                AddPoint(e.Points);
            }
        }

        protected void TouchEventTranslator_TouchUp(object sender, PointEventArgs e)
        {
            if (State == CaptureState.Capturing)
            {
                EndCapture();
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                _PointsCaptured = null;
            }

        }

        #endregion

        #region Private Methods

        private bool TryBeginCapture(IEnumerable<KeyValuePair<int, Point>> FirstTouch)
        {

            // Create capture args so we can notify subscribers that capture has started and allow them to cancel if they want.
            PointsCapturedEventArgs captureStartedArgs = new PointsCapturedEventArgs(FirstTouch.Select(p => p.Value).ToArray());
            OnCaptureStarted(captureStartedArgs);
            if (captureStartedArgs.Cancel)
                return false;
            if (captureStartedArgs.InterceptTouchInput && StartInterceptTouchInput != null)
                StartInterceptTouchInput(this, EventArgs.Empty);

            State = CaptureState.Capturing;

            // Clear old gesture from point list so we can start adding the new captures points to the list 
            _PointsCaptured = new Dictionary<int, List<Point>>(FirstTouch.Count());
            if (GestureSign.Common.Configuration.AppConfig.IsOrderByLocation)
            {
                foreach (KeyValuePair<int, Point> rawTouchData in FirstTouch.OrderBy(p => p.Value.X))
                {
                    if (!_PointsCaptured.ContainsKey(rawTouchData.Key))
                        _PointsCaptured.Add(rawTouchData.Key, new List<Point>(30));
                }
            }
            else
            {
                foreach (KeyValuePair<int, Point> rawTouchData in FirstTouch)
                {
                    if (!_PointsCaptured.ContainsKey(rawTouchData.Key))
                        _PointsCaptured.Add(rawTouchData.Key, new List<Point>(30));
                }
            }
            AddPoint(FirstTouch);
            return true;
        }

        private void EndCapture()
        {

            // Create points capture event args, to be used to send off to event subscribers or to simulate original Touch event
            PointsCapturedEventArgs pointsInformation = new PointsCapturedEventArgs(new List<List<Point>>(_PointsCaptured.Values), State);

            // Notify subscribers that capture has ended （draw end）
            OnCaptureEnded();
            State = CaptureState.Ready;
            // Notify PointsCaptured event subscribers that points have been captured.
            //CaptureWindow GetGestureName
            OnBeforePointsCaptured(pointsInformation);

            if (!pointsInformation.Cancel)
            {
                if (GestureSign.Common.Configuration.AppConfig.Teaching)
                {

                    bool createdSetting;
                    using (System.Threading.Mutex setting = new System.Threading.Mutex(false, "GestureSignSetting", out createdSetting))//true
                    {
                        if (createdSetting)
                        {
                            if (System.IO.File.Exists("GestureSign.exe"))
                                using (Process daemon = new Process())
                                {
                                    daemon.StartInfo.FileName = "GestureSign.exe";
                                    daemon.StartInfo.Arguments = "/L";
                                    // pipeClient.StartInfo.Arguments =            
                                    daemon.StartInfo.UseShellExecute = false;
                                    daemon.Start();
                                    daemon.WaitForInputIdle();
                                }
                        }
                    }
                    Tuple<string, List<List<Point>>> message =
                        new Tuple<string, List<List<Point>>>(GestureSign.Common.Gestures.GestureManager.Instance.GestureName, new List<List<Point>>(_PointsCaptured.Values));
                    GestureSign.Common.InterProcessCommunication.NamedPipe.SendMessage(message, "GestureSignSetting");
                    Input.TouchCapture.Instance.DisableTouchCapture();

                }
                OnAfterPointsCaptured(pointsInformation);
            }

        }

        private void CancelCapture(int num)
        {
            // Notify subscribers that gesture capture has been canceled
            OnCaptureCanceled(new PointsCapturedEventArgs(new List<List<Point>>(_PointsCaptured.Values), State));
        }

        private void AddPoint(IEnumerable<KeyValuePair<int, Point>> Point)
        {
            bool getNewPoint = false;
            foreach (KeyValuePair<int, Point> p in Point)
            {                // Don't accept point if it's within specified distance of last point unless it's the first point
                if (_PointsCaptured.ContainsKey(p.Key))
                {
                    if (_PointsCaptured[p.Key].Count() > 0 &&
                   GestureSign.PointPatterns.PointPatternMath.GetDistance(_PointsCaptured[p.Key].Last(), p.Value) < GestureSign.Common.Configuration.AppConfig.MinimumPointDistance)
                        continue;
                    getNewPoint = true;
                    // Add point to captured points list
                    _PointsCaptured[p.Key].Add(p.Value);
                }
            }
            if (getNewPoint)
            {

                // Notify subscribers that point has been captured
                OnPointCaptured(new PointsCapturedEventArgs(new List<List<Point>>(_PointsCaptured.Values), Point.Select(p => p.Value).ToArray(), State));
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
            // Ensure that the Touch hook is enabled, unless the user has selected to disable gestures
            if (State != CaptureState.UserDisabled)
                State = CaptureState.Ready;
        }

        public void DisableTouchCapture()
        {
            if (State != CaptureState.UserDisabled)
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
            State = State == CaptureState.UserDisabled ? CaptureState.Ready : CaptureState.UserDisabled;
            StateChanged(this, new StateChangedEventArgs(State));

        }


        #endregion
    }
}
