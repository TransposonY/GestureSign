using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
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

            GestureManager.OnLoadGesturesCompleted += (o, e) => { if (ApplicationManager.FinishedLoading) Application.Current.Dispatcher.Invoke(Update); };
            ApplicationManager.OnLoadApplicationsCompleted += (o, e) => { if (GestureManager.FinishedLoading) Application.Current.Dispatcher.Invoke(Update); };

            if (GestureManager.FinishedLoading) Update();
        }

        public GestureItemProvider()
        {
            PatternMap = GestureManager.FinishedLoading ? GestureItems.ToDictionary(gi => gi.Name, gi => gi.PointPattern) : new Dictionary<string, PointPattern[]>();
            GlobalPropertyChanged += (sender, propertyName) =>
            {
                PatternMap = GestureItems.ToDictionary(gi => gi.Name, gi => gi.PointPattern);
                OnPropertyChanged(propertyName);
            };
        }

        public static ObservableCollection<GestureItem> GestureItems
        {
            get { return _gestureItems; }
            set { _gestureItems = value; }
        }

        public Dictionary<string, PointPattern[]> PatternMap { get; set; }

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
            IEnumerable<IGesture> results = GestureManager.Instance.Gestures.OrderBy(g => g.PointPatterns?.Max(p => p.Points.Count));
            var apps = ApplicationManager.Instance.GetAvailableUserApplications().Union(ApplicationManager.Instance.GetAllGlobalApplication()).ToList();

            foreach (var g in results)
            {
                var gesture = (Gesture)g;
                string result = string.Empty;
                foreach (IApplication application in apps)
                {
                    if (application.Actions.Exists(a => a.GestureName == gesture.Name))
                        result += $" {application.Name},";
                }
                result = result.TrimEnd(',');

                GestureItem newItem = new GestureItem()
                {
                    Applications = result,
                    PointPattern = gesture.PointPatterns,
                    Name = gesture.Name,
                };
                GestureItems.Add(newItem);
            }
            GlobalPropertyChanged?.Invoke(typeof(GestureItemProvider), nameof(PatternMap));
        }
    }
}
