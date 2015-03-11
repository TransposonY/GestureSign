using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using GestureSign.Common;
using GestureSign.Common.Gestures;
using GestureSign.Common.Plugins;
using System.Drawing;
using GestureSign.PointPatterns;
using GestureSign.Common.Input;

namespace GestureSign.Gestures
{
    public class GestureManager : ILoadable, IGestureManager
    {
        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        static readonly GestureManager _Instance = new GestureManager();

        // Create read/write list of IGestures to hold system gestures
        List<IGesture> _Gestures = new List<IGesture>();

        // Create PointPatternAnalyzer to process gestures when received
        PointPatternAnalyzer gestureAnalyzer = null;

        #endregion

        #region Public Instance Properties

        public string GestureName { get; set; }
        public IGesture[] Gestures
        {
            get
            {
                if (_Gestures == null)
                    _Gestures = new List<IGesture>();

                return _Gestures.ToArray();
            }
        }

        #endregion

        #region Constructors

        protected GestureManager()
        {
            if (!LoadGestures())
                _Gestures = new List<IGesture>();

            // Instantiate gesture analyzer using gestures loaded from file
            gestureAnalyzer = new PointPatternAnalyzer();//Gestures

            // Wireup event to Touch capture class to catch points captured
            Input.TouchCapture.Instance.BeforePointsCaptured += new PointsCapturedEventHandler(TouchCapture_BeforePointsCaptured);
            Input.TouchCapture.Instance.AfterPointsCaptured += new PointsCapturedEventHandler(TouchCapture_AfterPointsCaptured);

            // Reload gestures if options were saved
        }

        #endregion

        #region Public Type Properties

        public static GestureManager Instance
        {
            get { return _Instance; }
        }

        #endregion

        #region Events

        protected void TouchCapture_BeforePointsCaptured(object sender, PointsCapturedEventArgs e)
        {
            this.GestureName = GetGestureName(e.Points);
        }

        protected void TouchCapture_AfterPointsCaptured(object sender, PointsCapturedEventArgs e)
        {
            RecognizeGesture(e.Points, e.CapturePoint);
        }

        #endregion

        #region Custom Events

        // Define events to allow other classes to subscribe to
        public event RecognitionEventHandler GestureRecognized;
        public event RecognitionEventHandler GestureNotRecognized;

        public event GestureEventHandler GestureEdited;
        // Define protected method to notifiy subscribers of events
        protected virtual void OnGestureRecognized(RecognitionEventArgs e)
        {
            if (GestureRecognized != null) GestureRecognized(this, e);
        }

        protected virtual void OnGestureNotRecognized(RecognitionEventArgs e)
        {
            if (GestureNotRecognized != null) GestureNotRecognized(this, e);
        }

        protected virtual void OnGestureEdited(GestureEventArgs e)
        {
            if (GestureEdited != null) GestureEdited(this, e);
        }
        #endregion

        #region Private Methods

        private void RecognizeGesture(List<List<Point>> Points, Point[] CapturePoint)
        {
            // Fire recognized event if we found a gesture match, otherwise throw not recognized event
            if (GestureName != null)
                OnGestureRecognized(new RecognitionEventArgs(GestureName, Points, CapturePoint));
            else
                OnGestureNotRecognized(new RecognitionEventArgs(Points, CapturePoint));
        }

        #endregion

        #region Public Methods

        public void Load()
        {
            // Shortcut method to control singleton instantiation
        }

        public void AddGesture(IGesture Gesture)
        {
            _Gestures.Add(Gesture);
        }

        public bool LoadGestures()
        {
            try
            {
                // Load gestures from file, create empty list if load failed
                _Gestures = Configuration.IO.FileManager.LoadObject<List<IGesture>>(Path.Combine("Data", "Gestures.json"), new Type[] { typeof(Gesture) }, true);

                if (Gestures == null)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SaveGestures()
        {
            try
            {
                // Save gestures to file
                Configuration.IO.FileManager.SaveObject<List<IGesture>>(Gestures, Path.Combine("Data", "Gestures.json"));

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetGestureName(List<List<Point>> Points)
        {
            // Get closest match, if no match, exit method
            return GetGestureSetNameMatch(Points);
        }

        public string GetGestureSetNameMatch(List<List<Point>> Points)//PointF[]
        {
            if (Points.Count == 0 || Gestures.Length == 0) return null;
            // Update gesture analyzer with latest gestures and get gesture match from current points array
            // Comparison results are sorted descending from highest to lowest probability
            IEnumerable<IGesture> gestures = Gestures.Where(g => g.Points.Count == Points.Count);
            List<PointPatternMatchResult>[] comparisonResults = new List<PointPatternMatchResult>[Points.Count];
            for (int i = 0; i < Points.Count; i++)
            {
                gestureAnalyzer.PointPatternSet = gestures.Select(gesture => new PointPatternAnalyzer.PointsPatternSet(gesture.Name, gesture.Points[i].ToArray()));
                comparisonResults[i] = new List<PointPatternMatchResult>(5);
                comparisonResults[i].AddRange(gestureAnalyzer.GetPointPatternMatchResults(Points[i].ToArray()));
                comparisonResults[i].RemoveAll(ppmr => ppmr.Probability < 80);
                // Exit if we didn't find a high probability match
                if (comparisonResults[i] == null || comparisonResults[i].Count <= 0)
                    return null;		// No close enough match. Do nothing with drawn gesture
            }
            for (int i = 1; i < comparisonResults.Length; i++)
                for (int j = 0; j < comparisonResults[0].Count; j++)
                {
                    //int index = -1;
                    if (comparisonResults[i].Exists(cr => cr.Name == comparisonResults[0][j].Name))
                        comparisonResults[0][j].Probability += comparisonResults[i].Find(cr => cr.Name == comparisonResults[0][j].Name).Probability;
                    else
                    {
                        comparisonResults[0].RemoveAt(j);
                        j--;
                    }
                }
            // Grab top result from gesture comparison
            if (comparisonResults[0].Count == 0) return null;
            return comparisonResults[0].OrderByDescending(ppmr => ppmr.Probability).First().Name;
        }

        public string[] GetAvailableGestures()
        {
            return Gestures.OrderBy(g => g.Name).GroupBy(g => g.Name).Select(g => g.Key).ToArray();
        }

        public bool GestureExists(string GestureName)
        {
            return _Gestures.Exists(g => g.Name.ToLower() == GestureName.Trim().ToLower());
        }

        public IGesture GetNewestGestureSample(string GestureName)
        {
            if (String.IsNullOrEmpty(GestureName)) return null;
            return Gestures.LastOrDefault(g => g.Name.ToLower() == GestureName.Trim().ToLower());
        }

        public IGesture GetNewestGestureSample()
        {
            return GetNewestGestureSample(this.GestureName);
        }

        public void DeleteGesture(string GestureName)
        {
            _Gestures.RemoveAll(g => g.Name.Trim() == GestureName.Trim());
        }

        public void RenameGesture(string gestureName, string newGestureName)
        {
            if (gestureName.Equals(newGestureName)) return;
            _Gestures.Find(g => g.Name == gestureName).Name = newGestureName;

            OnGestureEdited(new GestureEventArgs(gestureName, newGestureName));
        }
        #endregion
    }
}
