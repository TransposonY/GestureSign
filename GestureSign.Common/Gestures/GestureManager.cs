using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using GestureSign.Common.Configuration;
using GestureSign.PointPatterns;
using GestureSign.Common.Input;
using Newtonsoft.Json;

namespace GestureSign.Common.Gestures
{
    public class GestureManager : IGestureManager
    {
        #region Private Variables

        private const int ProbabilityThreshold = 80;
        private const int GestureStackTimeout = 800;

        private int _gestureLevel = 0;

        // Create variable to hold the only allowed instance of this class
        private static GestureManager _instance;

        // Create read/write list of IGestures to hold system gestures
        private List<IGesture> _Gestures;

        // Create PointPatternAnalyzer to process gestures when received
        PointPatternAnalyzer gestureAnalyzer = null;

        private bool _isGestureStackTimeout;
        private int? _lastGestureTime;
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

        public Task LoadingTask { get; }

        #endregion

        #region Constructors

        protected GestureManager()
        {
            LoadingTask = LoadGestures();
            // Instantiate gesture analyzer using gestures loaded from file
            gestureAnalyzer = new PointPatternAnalyzer();//Gestures
        }

        #endregion

        #region Public Type Properties

        public static GestureManager Instance
        {
            get { return _instance ?? (_instance = new GestureManager()); }
        }

        #endregion

        #region Events

        protected void PointCapture_CaptureStarted(object sender, PointsCapturedEventArgs e)
        {
            if (_lastGestureTime != null && Environment.TickCount - _lastGestureTime.Value > GestureStackTimeout)
                _isGestureStackTimeout = true;
        }

        protected void PointCapture_BeforePointsCaptured(object sender, PointsCapturedEventArgs e)
        {
            var pointCapture = (IPointCapture)sender;

            if (_isGestureStackTimeout)
            {
                _lastGestureTime = null;
                _isGestureStackTimeout = false;

                _gestureLevel = 0;
                _gestureMatchResult = null;
            }

            if (pointCapture.Mode == CaptureMode.Training)
            {
                _gestureLevel = 0;
                _gestureMatchResult = null;
            }

            var sourceGesture = _gestureLevel == 0 ? _Gestures : _gestureMatchResult;
            GestureName = GetGestureSetNameMatch(e.Points.Select(l => l.ToArray()).ToArray(), sourceGesture, _gestureLevel, out _gestureMatchResult);

            if (pointCapture.Mode != CaptureMode.Training)
            {
                if (_gestureMatchResult != null && _gestureMatchResult.Count != 0)
                {
                    _gestureLevel++;
                    _lastGestureTime = Environment.TickCount;
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
        public static event EventHandler GestureSaved;

        #endregion

        #region Private Methods

        private bool LoadDefaults()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", Constants.GesturesFileName);
            _Gestures = LoadGesturesFromFile(path);

            return _Gestures != null;
        }

        private bool LoadBackup()
        {
            var directory = new DirectoryInfo(AppConfig.BackupPath);
            if (directory.Exists)
            {
                var files = directory.EnumerateFiles("*" + Constants.GesturesExtension).OrderByDescending(f => f.LastWriteTime);
                foreach (var file in files)
                {
                    var gestures = LoadGesturesFromFile(file.FullName);
                    if (gestures != null)
                    {
                        _Gestures = gestures;
                        return true;
                    }
                }
            }
            return false;
        }

        private static string GetRandomString(Random random, int length)
        {
            string input = "abcdefghijklmnopqrstuvwxyz0123456789";
            var chars = Enumerable.Range(0, length).Select(x => input[random.Next(0, input.Length)]);
            return new string(chars.ToArray());
        }

        #endregion

        #region Public Methods

        public void Load(IPointCapture pointCapture)
        {
            // Shortcut method to control singleton instantiation

            // Wireup event to Touch capture class to catch points captured       
            if (pointCapture != null)
            {
                pointCapture.BeforePointsCaptured += PointCapture_BeforePointsCaptured;
                pointCapture.CaptureStarted += PointCapture_CaptureStarted; ;
            }
        }

        public void AddGesture(IGesture Gesture)
        {
            _Gestures.Add(Gesture);
        }

        public Task LoadGestures()
        {
            Action<bool> loadCompleted =
                   result =>
                   {
                       if (!result)
                           if (!LoadBackup())
                               if (!LoadDefaults())
                                   _Gestures = new List<IGesture>();
                       OnLoadGesturesCompleted?.Invoke(this, EventArgs.Empty);
                   };

            var startLoading = Task.Run(() =>
            {
                try
                {
                    string path = Path.Combine(AppConfig.ApplicationDataPath, Constants.GesturesFileName);
                    var gestures = LoadGesturesFromFile(path);

                    if (gestures != null)
                    {
                        if (gestures.Count == 0 || gestures[0].PointPatterns == null)
                        {
                            List<LegacyGesture> legacyGestures = FileManager.LoadObject<List<LegacyGesture>>(path, true);

                            foreach (var gesture in legacyGestures)
                            {
                                if (gesture.Points != null)
                                    gesture.PointPatterns = new[] { new PointPattern(gesture.Points) };
                            }
                            _Gestures = legacyGestures.Cast<IGesture>().ToList();
                        }
                        else
                        {
                            _Gestures = gestures;
                        }
                    }


                    return _Gestures != null;
                }
                catch
                {
                    return false;
                }
            });

            return startLoading.ContinueWith(antecendent => loadCompleted(antecendent.Result));
        }

        public bool SaveGestures()
        {
            try
            {
                // Save gestures to file
                bool flag = Configuration.FileManager.SaveObject(Gestures, Path.Combine(AppConfig.ApplicationDataPath, Constants.GesturesFileName));
                if (flag)
                {
                    GestureSaved?.Invoke(this, EventArgs.Empty);
                }
                return flag;
            }
            catch
            {
                return false;
            }
        }

        public static List<IGesture> LoadGesturesFromFile(string filePath, bool throwException = false)
        {
            if (!File.Exists(filePath)) return null;

            FileManager.WaitFile(filePath);

            List<IGesture> gestureList = new List<IGesture>();
            try
            {
                string json = File.ReadAllText(filePath);
                JsonTextReader reader = new JsonTextReader(new StringReader(json));
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        Gesture gesture = new Gesture();
                        List<PointPattern> pointPatternList = new List<PointPattern>();
                        while (reader.Read())
                        {
                            if (reader.TokenType != JsonToken.PropertyName) continue;
                            switch ((string)reader.Value)
                            {
                                case nameof(Gesture.Name):
                                    {
                                        gesture.Name = reader.ReadAsString();
                                        break;
                                    }
                                case nameof(Gesture.PointPatterns):
                                    {
                                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                        {
                                            if (reader.TokenType == JsonToken.StartArray)
                                            {
                                                var strokeList = new List<Point[]>();
                                                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                                {
                                                    if (reader.TokenType == JsonToken.StartArray)
                                                    {
                                                        var stroke = new List<Point>();
                                                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                                        {
                                                            if (reader.TokenType == JsonToken.String)
                                                            {
                                                                var num = ((string)reader.Value).Split(',');
                                                                stroke.Add(new Point(Convert.ToInt32(num[0]), Convert.ToInt32(num[1])));
                                                            }
                                                        }
                                                        strokeList.Add(stroke.ToArray());
                                                    }
                                                }
                                                PointPattern pointPattern = new PointPattern(strokeList.ToArray());
                                                pointPatternList.Add(pointPattern);
                                            }
                                        }
                                        gesture.PointPatterns = pointPatternList.ToArray();
                                        break;
                                    }
                            }
                            if (gesture.Name != null && gesture.PointPatterns != null)
                            {
                                gestureList.Add(gesture);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logging.LogException(e);
                if (throwException)
                    throw;
                return null;
            }

            return gestureList;
        }

        public string GetGestureSetNameMatch(Point[][] points, List<IGesture> sourceGestures, int sourceGestureLevel, out List<IGesture> matching)//PointF[]
        {
            if (points.Length == 0 || sourceGestures == null || sourceGestures.Count == 0)
            { matching = null; return null; }
            // Update gesture analyzer with latest gestures and get gesture match from current points array
            // Comparison results are sorted descending from highest to lowest probability
            var gestures =
                sourceGestures.Where(g =>
                        g.PointPatterns != null && g.PointPatterns.Length > sourceGestureLevel &&
                        g.PointPatterns[sourceGestureLevel].Points != null &&
                        g.PointPatterns[sourceGestureLevel].Points.Length == points.Length).ToList();
            List<PointPatternMatchResult>[] comparisonResults = new List<PointPatternMatchResult>[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                gestureAnalyzer.PointPatternSet = gestures.Select(gesture => new PointsPatternSet(gesture.Name, gesture.PointPatterns[sourceGestureLevel].Points[i]));
                comparisonResults[i] = new List<PointPatternMatchResult>(gestures.Count);
                comparisonResults[i].AddRange(gestureAnalyzer.GetPointPatternMatchResults(points[i]));
            }

            var numbers = Enumerable.Range(0, gestures.Count);
            numbers = comparisonResults.Aggregate(numbers, (current, matchResultsList) => current.Where(i => matchResultsList[i].Probability > ProbabilityThreshold).ToList());

            List<IGesture> matchingResult = new List<IGesture>();
            List<KeyValuePair<string, double>> recognizedResult = new List<KeyValuePair<string, double>>();

            foreach (var number in numbers)
            {
                var gesture = gestures[number];
                if (gesture.PointPatterns.Length > sourceGestureLevel + 1)
                {
                    matchingResult.Add(gesture);
                }
                else
                {
                    double probability = comparisonResults.Sum(matchResultsList => matchResultsList[number].Probability);

                    recognizedResult.Add(new KeyValuePair<string, double>(gesture.Name, probability));
                }
            }

            matching = matchingResult.Count == 0 ? null : matchingResult;
            return recognizedResult.Count == 0 ? null : recognizedResult.OrderByDescending(r => r.Value).First().Key;
        }

        public string GetMostSimilarGestureName(PointPattern[] pointPattern)
        {
            string matchName = null;
            List<IGesture> matchGestures = null;
            for (int i = 0; i < pointPattern.Length;)
            {
                matchName = GetGestureSetNameMatch(pointPattern[i].Points, matchGestures ?? _Gestures, i, out matchGestures);

                if (++i < pointPattern.Length && matchGestures == null)
                    return null;
            }
            return matchName;
        }

        public string GetMostSimilarGestureName(IGesture gesture)
        {
            return GetMostSimilarGestureName(gesture.PointPatterns);
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

        public string GetNewGestureName()
        {
            Random random = new Random();
            string newName;
            do
            {
                newName = GetRandomString(random, 6);
            } while (GestureExists(newName));
            return newName;
        }

        public string GetNewGestureId(PointPattern[] pointPatterns)
        {
            string features = "";
            foreach (var pattern in pointPatterns)
            {
                features += GetPatternFeatures(pattern.Points);
            }
            int num = 0;
            string newId = features;
            while (GestureExists(newId))
            {
                newId = features + num;
                num++;
            };
            return newId;
        }

        public static string GetPatternFeatures(Point[][] pattern)
        {
            string output = "";
            for (int i = 0; i < pattern.Length; i++)
            {
                var currentStroke = pattern[i];
                if (currentStroke.Length == 1)
                {
                    output += 0;
                    continue;
                }
                var strokeFeature = PointPatternMath.GetPointArrayAngularMargins(PointPatternMath.GetInterpolatedPointArray(currentStroke, 9));
                uint feature = 0;
                for (int j = 0; j < strokeFeature.Length; j++)
                {
                    var feat = strokeFeature[j] / Math.PI + 1 - 3.0 / 8.0;
                    if (feat < 0)
                    {
                        feat = 2 + feat;
                    }
                    feature = feature << 4;
                    feature |= (uint)(feat / 2 * 8) << 1;
                    feature++;
                }
                output += feature.ToString("x8");
            }

            return output;
        }

        #endregion
    }
}
