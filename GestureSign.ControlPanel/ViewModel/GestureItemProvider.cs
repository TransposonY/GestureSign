using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.ControlPanel.Common;

namespace GestureSign.ControlPanel.ViewModel
{
    public class GestureItemProvider : INotifyPropertyChanged
    {
        private static ObservableCollection<GestureItem> _gestureItems;
        private static event EventHandler<string> GlobalPropertyChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        static GestureItemProvider()
        {
            _gestureItems = new ObservableCollection<GestureItem>();

            GestureManager.GestureSaved += (o, e) => { Update(); };

            ApplicationManager.Instance.LoadingTask.ContinueWith((task) =>
            {
                ApplicationManager.ApplicationSaved += (o, e) =>
                {
                    Application.Current.Dispatcher.Invoke(Update);
                };
                // Action summaries resolve continuous gesture names through the catalog.
                ContinuousGestureManager.Saved += (o, e) => Application.Current.Dispatcher.InvokeAsync(Update);
                ContinuousGestureManager.LoadCompleted += (o, e) => Application.Current.Dispatcher.InvokeAsync(Update);
                GestureManager.Instance.LoadingTask.Wait();
                Application.Current.Dispatcher.Invoke(Update);
            });
        }

        public GestureItemProvider()
        {
            GlobalPropertyChanged += (sender, propertyName) =>
            {
                OnPropertyChanged(propertyName);
            };
        }

        public static ObservableCollection<GestureItem> GestureItems
        {
            get { return _gestureItems; }
            set { _gestureItems = value; }
        }

        /// <summary>Raised on the UI thread after gestures, applications or continuous gestures were (re)loaded or saved.</summary>
        public static event EventHandler GestureMapChanged;

        public static Dictionary<string, GestureItem> GestureMap { get; private set; } = new Dictionary<string, GestureItem>();

        public Dictionary<string, GestureItem> InstanceGestureMap
        {
            get
            {
                return GestureMap;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void Update()
        {
            if (_gestureItems == null)
                _gestureItems = new ObservableCollection<GestureItem>();
            else
                _gestureItems.Clear();

            // Get all available gestures from gesture manager
            var apps = ApplicationManager.Instance.Applications.Where(app => !(app is IgnoredApp)).ToList();

            var color = (Color)Application.Current.Resources["HighlightColor"];

            foreach (var g in GestureManager.Instance.Gestures)
            {
                var gesture = (Gesture)g;
                string result = string.Empty;
                var applicationNames = new List<string>();
                int actionCount = 0;
                foreach (IApplication application in apps)
                {
                    int used = application.Actions.Count(a => a.GestureName == gesture.Name);
                    if (used > 0)
                    {
                        result += $" {application.Name},";
                        applicationNames.Add(application.Name);
                        actionCount += used;
                    }
                }
                result = result.TrimEnd(',');

                var shape = GestureShapeAnalyzer.Analyze(gesture.PointPatterns);

                GestureItem newItem = new GestureItem()
                {
                    GestureImage = GestureImage.CreateImage(gesture.PointPatterns, new Size(60, 60), color),
                    Features = GestureManager.Instance.GetNewGestureId(gesture.PointPatterns),
                    PatternCount = gesture?.PointPatterns.Max(p => p.Points.Length) ?? 0,
                    Applications = result,
                    ApplicationNames = applicationNames.ToArray(),
                    ActionCount = actionCount,
                    MovingStrokeCount = gesture.PointPatterns.Length == 0 ? 0 : gesture.PointPatterns[0].Points.Count(s => s.Length > 1),
                    Description = GestureListFilter.Describe(shape),
                    Gesture = gesture
                };
                GestureItems.Add(newItem);
            }
            GestureMap = GestureItems.ToDictionary(gi => gi.Gesture.Name);
            GlobalPropertyChanged?.Invoke(typeof(GestureItemProvider), nameof(InstanceGestureMap));
            GestureMapChanged?.Invoke(typeof(GestureItemProvider), EventArgs.Empty);
        }
    }
}
