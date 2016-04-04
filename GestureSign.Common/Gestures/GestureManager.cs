using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using GestureSign.Common;
using GestureSign.Common.Plugins;
using System.Drawing;
using System.Threading.Tasks;
using GestureSign.Common.Configuration;
using GestureSign.PointPatterns;
using GestureSign.Common.Input;

namespace GestureSign.Common.Gestures
{
    public class GestureManager : IGestureManager
    {
        #region Private Variables

        private const int ProbabilityThreshold = 80;

        private int _gestureLevel = 0;
        // Create variable to hold the only allowed instance of this class
        private static GestureManager _instance;

        // Create read/write list of IGestures to hold system gestures
        List<IGesture> _Gestures = new List<IGesture>();

        // Create PointPatternAnalyzer to process gestures when received
        PointPatternAnalyzer gestureAnalyzer = null;

        private List<IGesture> _gestureMatchResult;

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
            Action<bool> loadCompleted =
                   result =>
                   {
                       if (!result)
                           if (!LoadDefaults())
                               _Gestures = new List<IGesture>();
                       if (OnLoadGesturesCompleted != null) OnLoadGesturesCompleted(this, EventArgs.Empty);
                       FinishedLoading = true;
                   };
            LoadGestures().ContinueWith(antecendent => loadCompleted(antecendent.Result));
            // Instantiate gesture analyzer using gestures loaded from file
            gestureAnalyzer = new PointPatternAnalyzer();//Gestures
        }

        #endregion

        #region Public Type Properties

        public static GestureManager Instance
        {
            get { return _instance ?? (_instance = new GestureManager()); }
        }

        public static bool FinishedLoading { get; set; }

        #endregion

        #region Events

        protected void TouchCapture_BeforePointsCaptured(object sender, PointsCapturedEventArgs e)
        {
            var touchCapture = (ITouchCapture)sender;

            if (touchCapture.Mode == CaptureMode.Training)
            {
                if (touchCapture.OverlayGesture)
                {
                    _gestureLevel++;
                }
                else
                {
                    _gestureLevel = 0;
                    _gestureMatchResult = null;
                }
            }

            if (e.GestureTimeout)
            {
                _gestureLevel = 0;
                _gestureMatchResult = null;
            }

            var sourceGesture = _gestureLevel == 0 ? _Gestures : _gestureMatchResult;
            GestureName = GetGestureSetNameMatch(e.Points, sourceGesture, out _gestureMatchResult);

            if (touchCapture.Mode != CaptureMode.Training)
            {
                if (_gestureMatchResult != null && _gestureMatchResult.Count != 0)
                {
                    _gestureLevel++;
                    e.Delay = true;
                }
                else
                {
                    _gestureLevel = 0;
                    _gestureMatchResult = null;
                }
            }
        }

        #endregion

        #region Custom Events

        public static event EventHandler OnLoadGesturesCompleted;
        // Define events to allow other classes to subscribe to
        public static event GestureEventHandler GestureEdited;
        public static event EventHandler GestureSaved;
        // Define protected method to notifiy subscribers of events

        protected virtual void OnGestureEdited(GestureEventArgs e)
        {
            if (GestureEdited != null) GestureEdited(this, e);
        }
        #endregion

        #region Private Methods

        private bool LoadDefaults()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Defaults\Gestures.gest");

            var gestures = FileManager.LoadObject<List<Gesture>>(path, true);
            _Gestures = gestures?.Cast<IGesture>().ToList();

            return _Gestures != null;
        }

        #endregion

        #region Public Methods

        public void Load(ITouchCapture touchCapture)
        {
            // Shortcut method to control singleton instantiation

            // Wireup event to Touch capture class to catch points captured       
            if (touchCapture != null)
            {
                touchCapture.BeforePointsCaptured += new PointsCapturedEventHandler(TouchCapture_BeforePointsCaptured);
            }
        }

        public void AddGesture(IGesture Gesture)
        {
            _Gestures.Add(Gesture);
        }

        public Task<bool> LoadGestures()
        {
            return Task.Run(() =>
            {
                try
                {
                    // Load gestures from file, create empty list if load failed
                    var gestures = FileManager.LoadObject<List<Gesture>>(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "GestureSign", "Gestures.gest"), true);
                    if (gestures == null)
                    {
                        _Gestures = FileManager.LoadObject<List<IGesture>>(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign", "Gestures.json"), new Type[] { typeof(Gesture) }, true);
                        if (_Gestures != null)
                            SaveGestures(false);
                    }
                    else
                    {
                        if (gestures.Count != 0 && gestures[0].PointPatterns == null)
                        {
                            List<LegacyGesture> legacyGestures = FileManager.LoadObject<List<LegacyGesture>>(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign", "Gestures.gest"), true);

                            foreach (var gesture in legacyGestures)
                            {
                                if (gesture.Points != null)
                                    gesture.PointPatterns = new[] { new PointPattern(gesture.Points) };
                            }
                            _Gestures = legacyGestures.Cast<IGesture>().ToList();
                        }
                        else
                        {
                            _Gestures = gestures.Cast<IGesture>().ToList();
                        }
                    }


                    return _Gestures != null;
                }
                catch
                {
                    return false;
                }
            });
        }

        public bool SaveGestures(bool notice = true)
        {
            try
            {
                // Save gestures to file
                bool flag = Configuration.FileManager.SaveObject(Gestures, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign", "Gestures.gest"));
                if (flag)
                {
                    GestureSaved?.Invoke(this, EventArgs.Empty);
                    if (notice)
                        InterProcessCommunication.NamedPipe.SendMessageAsync("LoadGestures", "GestureSignDaemon");
                }
                return flag;
            }
            catch
            {
                return false;
            }
        }

        public string GetGestureSetNameMatch(List<List<Point>> points, List<IGesture> sourceGestures, out List<IGesture> matchResult)//PointF[]
        {
            if (points.Count == 0 || sourceGestures == null || sourceGestures.Count == 0)
            { matchResult = null; return null; }
            // Update gesture analyzer with latest gestures and get gesture match from current points array
            // Comparison results are sorted descending from highest to lowest probability
            var gestures = sourceGestures.Where(g => g.PointPatterns.Length > _gestureLevel && g.PointPatterns[_gestureLevel].Points != null && g.PointPatterns[_gestureLevel].Points.Count == points.Count).ToList();
            List<PointPatternMatchResult>[] comparisonResults = new List<PointPatternMatchResult>[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                gestureAnalyzer.PointPatternSet = gestures.Select(gesture => new PointPatternAnalyzer.PointsPatternSet(gesture.Name, gesture.PointPatterns[_gestureLevel].Points[i].ToArray()));
                comparisonResults[i] = new List<PointPatternMatchResult>(gestures.Count);
                comparisonResults[i].AddRange(gestureAnalyzer.GetPointPatternMatchResults(points[i].ToArray()));
            }

            List<int> numbers = new List<int>(gestures.Count);
            for (int j = 0; j < gestures.Count; j++)
            {
                numbers.Add(j);
            }

            numbers = comparisonResults.Aggregate(numbers, (current, matchResultsList) => current.Where(i => matchResultsList[i].Probability > ProbabilityThreshold).ToList());

            List<IGesture> result = new List<IGesture>();
            List<KeyValuePair<string, double>> recognizedResult = new List<KeyValuePair<string, double>>();

            foreach (var number in numbers)
            {
                var gesture = gestures[number];
                if (gesture.PointPatterns.Length > _gestureLevel + 1)
                {
                    result.Add(gesture);
                }
                else
                {
                    double probability = comparisonResults.Sum(matchResultsList => matchResultsList[number].Probability);

                    recognizedResult.Add(new KeyValuePair<string, double>(gesture.Name, probability));
                }
            }

            matchResult = result.Count == 0 ? null : result;
            return recognizedResult.Count == 0 ? null : recognizedResult.OrderByDescending(r => r.Value).First().Key;
        }

        public string[] GetAvailableGestures()
        {
            return Gestures.OrderBy(g => g.Name).GroupBy(g => g.Name).Select(g => g.Key).ToArray();
        }

        public bool GestureExists(string gestureName)
        {
            return _Gestures.Exists(g => String.Equals(g.Name, gestureName, StringComparison.Ordinal));
        }

        public IGesture GetNewestGestureSample(string gestureName)
        {
            return String.IsNullOrEmpty(gestureName) ? null : Gestures.LastOrDefault(g => String.Equals(g.Name, gestureName, StringComparison.Ordinal));
        }

        public IGesture GetNewestGestureSample()
        {
            return GetNewestGestureSample(this.GestureName);
        }

        public void DeleteGesture(string gestureName)
        {
            _Gestures.RemoveAll(g => g.Name.Trim() == gestureName.Trim());
        }

        public void RenameGesture(string gestureName, string newGestureName)
        {
            _Gestures.Find(g => g.Name == gestureName).Name = newGestureName;

            OnGestureEdited(new GestureEventArgs(gestureName, newGestureName));
        }
        #endregion
    }
}
