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
using GestureSign.Gestures;

namespace GestureSign.Common.Gestures
{
    public class GestureManager : IGestureManager
    {
        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        private static GestureManager _instance;

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
            e.GestureName = this.GestureName = GetGestureName(e.Points);
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
                    else _Gestures = gestures.Cast<IGesture>().ToList();


                    return _Gestures != null;
                }
                catch
                {
                    return false;
                }
            });
        }
        private bool LoadDefaults()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Defaults\Gestures.gest");

            var gestures = FileManager.LoadObject<List<Gesture>>(path, true);
            _Gestures = gestures == null ? null : gestures.Cast<IGesture>().ToList();

            if (_Gestures == null)
                return false;
            return true;
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
