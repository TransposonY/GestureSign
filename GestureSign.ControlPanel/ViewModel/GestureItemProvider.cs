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
                foreach (IApplication application in apps)
                {
                    if (application.Actions.Any(a => a.GestureName == gesture.Name))
                        result += $" {application.Name},";
                }
                result = result.TrimEnd(',');

                GestureItem newItem = new GestureItem()
                {
                    GestureImage = GestureImage.CreateImage(gesture.PointPatterns, new Size(60, 60), color),
                    Features = GestureManager.Instance.GetNewGestureId(gesture.PointPatterns),
                    PatternCount = gesture?.PointPatterns.Max(p => p.Points.Count) ?? 0,
                    Applications = result,
                    Gesture = gesture
                };
                GestureItems.Add(newItem);
            }
            GestureMap = GestureItems.ToDictionary(gi => gi.Gesture.Name);
            GlobalPropertyChanged?.Invoke(typeof(GestureItemProvider), nameof(InstanceGestureMap));
        }
    }
}
