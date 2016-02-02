﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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

namespace GestureSign.Daemon.Input
{
    public class TouchCapture : ILoadable, ITouchCapture
    {
        #region Private Variables

        // Create new Touch hook control to capture global input from Touch, and create an event translator to get formal events
        TouchEventTranslator TouchEventTranslator = new TouchEventTranslator();
        readonly MessageWindow messageWindow = new MessageWindow();

        Dictionary<int, List<Point>> _PointsCaptured = new Dictionary<int, List<Point>>(2);
        // Create variable to hold the only allowed instance of this class
        static readonly TouchCapture _Instance = new TouchCapture();

        private CaptureMode _mode = CaptureMode.Normal;

        #endregion

        #region Public Instance Properties

        // Create enumeration to identify Touch buttons
        public IntPtr MessageWindowHandle { get { return messageWindow.Handle; } }
        public MessageWindow MessageWindow { get { return messageWindow; } }
        public bool TemporarilyDisableCapture { get; set; }

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
            messageWindow.PointsIntercepted += new RawPointsDataMessageEventHandler(TouchEventTranslator.TranslateTouchEvent);
            TouchEventTranslator.TouchDown += (PointEventTranslator_TouchDown);
            TouchEventTranslator.TouchUp += (TouchEventTranslator_TouchUp);
            TouchEventTranslator.TouchMove += (TouchEventTranslator_TouchMove);

            messageWindow.OnForegroundChange += messageWindow_OnForegroundChange;

            ModeChanged += (o, e) => { if (e.Mode == CaptureMode.UserDisabled) messageWindow.InterceptTouchInput(false); };
        }



        #endregion

        #region Touch Events

        void messageWindow_OnForegroundChange(object sender, IntPtr e)
        {
            if (State != CaptureState.Ready || Mode != CaptureMode.Normal || e.Equals(IntPtr.Zero) ||
                Application.OpenForms.Count != 0 && e.Equals(Application.OpenForms[0].Handle))
                return;
            var systemWindow = new SystemWindow(e);
            var userApp = ApplicationManager.Instance.GetApplicationFromWindow(systemWindow, true);
            bool flag = userApp != null &&
                        (userApp.Any(app => app is UserApplication && ((UserApplication)app).InterceptTouchInput));

            messageWindow.InterceptTouchInput(flag);

        }

        protected void PointEventTranslator_TouchDown(object sender, PointEventArgs e)
        {
            // Can we begin a new gesture capture

            if (State == CaptureState.Ready || State == CaptureState.Capturing)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                // Try to begin capture process, if capture started then don't notify other applications of a Touch event, otherwise do
                if (!TryBeginCapture(e.Points))
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
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
            if (TemporarilyDisableCapture && Mode == CaptureMode.UserDisabled)
            {
                TemporarilyDisableCapture = false;
                ToggleUserDisableTouchCapture();
            }

            if (State == CaptureState.Capturing)
            {
                EndCapture();
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                _PointsCaptured = null;
            }

        }

        #endregion

        #region Private Methods

        private bool TryBeginCapture(IEnumerable<KeyValuePair<int, Point>> firstTouch)
        {

            // Create capture args so we can notify subscribers that capture has started and allow them to cancel if they want.
            PointsCapturedEventArgs captureStartedArgs = new PointsCapturedEventArgs(firstTouch.Select(p => p.Value).ToArray()) { Mode = Mode };
            OnCaptureStarted(captureStartedArgs);

            messageWindow.InterceptTouchInput(captureStartedArgs.InterceptTouchInput && Mode == CaptureMode.Normal);

            if (captureStartedArgs.Cancel && Mode != CaptureMode.Training)
                return false;

            State = CaptureState.Capturing;

            // Clear old gesture from point list so we can start adding the new captures points to the list 
            _PointsCaptured = new Dictionary<int, List<Point>>(firstTouch.Count());
            if (AppConfig.IsOrderByLocation)
            {
                foreach (KeyValuePair<int, Point> rawTouchData in firstTouch.OrderBy(p => p.Value.X))
                {
                    if (!_PointsCaptured.ContainsKey(rawTouchData.Key))
                        _PointsCaptured.Add(rawTouchData.Key, new List<Point>(30));
                }
            }
            else
            {
                foreach (KeyValuePair<int, Point> rawTouchData in firstTouch)
                {
                    if (!_PointsCaptured.ContainsKey(rawTouchData.Key))
                        _PointsCaptured.Add(rawTouchData.Key, new List<Point>(30));
                }
            }
            AddPoint(firstTouch);
            return true;
        }

        private async void EndCapture()
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
                if (Mode == CaptureMode.Training && !(_PointsCaptured.Count == 1 && _PointsCaptured.Values.First().Count == 1))
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

                    Tuple<string, List<List<Point>>> message =
                        new Tuple<string, List<List<Point>>>(GestureManager.Instance.GestureName, new List<List<Point>>(_PointsCaptured.Values));
                    if (await NamedPipe.SendMessageAsync(message, "GestureSignControlPanel"))
                        Instance.DisableTouchCapture();

                }
                // Fire recognized event if we found a gesture match, otherwise throw not recognized event
                if (pointsInformation.GestureName != null)
                    OnGestureRecognized(new RecognitionEventArgs(pointsInformation.GestureName, pointsInformation.Points,
                        pointsInformation.CapturePoint)
                    { Mode = Mode });
                else
                    OnGestureNotRecognized(new RecognitionEventArgs(pointsInformation.Points, pointsInformation.CapturePoint));

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
                    if (_PointsCaptured[p.Key].Any() &&
                   PointPatternMath.GetDistance(_PointsCaptured[p.Key].Last(), p.Value) < AppConfig.MinimumPointDistance)
                        continue;
                    getNewPoint = true;
                    // Add point to captured points list
                    _PointsCaptured[p.Key].Add(p.Value);
                }
            }
            if (getNewPoint)
            {

                // Notify subscribers that point has been captured
                OnPointCaptured(new PointsCapturedEventArgs(new List<List<Point>>(_PointsCaptured.Values), Point.Select(p => p.Value).ToArray(), State) { Mode = Mode });
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
